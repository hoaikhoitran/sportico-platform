using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Services.Payments
{
    public class PayOsService : IPayOsService
    {
        private readonly HttpClient _httpClient;
        private readonly PayOsSettings _settings;
        private readonly ILogger<PayOsService> _logger;

        public PayOsService(
            HttpClient httpClient,
            IOptions<PayOsSettings> settings,
            ILogger<PayOsService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<CreatePayOsPaymentResult> CreatePaymentLinkAsync(
            CreatePayOsPaymentRequest request)
        {
            ValidateSettings();

            var expiredAtUnix = DateTimeOffset.UtcNow
                .AddMinutes(_settings.PaymentLinkExpireMinutes)
                .ToUnixTimeSeconds();

            var signatureData =
                $"amount={request.Amount}" +
                $"&cancelUrl={_settings.CancelUrl}" +
                $"&description={request.Description}" +
                $"&orderCode={request.OrderCode}" +
                $"&returnUrl={_settings.ReturnUrl}";

            var signature = GenerateHmacSha256(
                signatureData,
                _settings.ChecksumKey);

            var body = new
            {
                orderCode = request.OrderCode,
                amount = request.Amount,
                description = request.Description,
                buyerName = request.BuyerName,
                buyerEmail = request.BuyerEmail,
                buyerPhone = request.BuyerPhone,
                cancelUrl = _settings.CancelUrl,
                returnUrl = _settings.ReturnUrl,
                expiredAt = expiredAtUnix,
                signature
            };

            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "/v2/payment-requests");

            httpRequest.Headers.Add("x-client-id", _settings.ClientId);
            httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
            httpRequest.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(httpRequest);

            var rawJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "payOS create payment failed with status {StatusCode}: {RawJson}",
                    (int)response.StatusCode,
                    rawJson);

                throw new FailureException(
                    ErrorCodes.PayOsCreatePaymentFailed,
                    "Failed to create payOS payment link",
                    new List<string>
                    {
                        $"StatusCode: {(int)response.StatusCode}",
                        rawJson
                    });
            }

            using var document = JsonDocument.Parse(rawJson);

            var root = document.RootElement;

            var code = root.GetProperty("code").GetString();

            if (code != "00")
            {
                _logger.LogError(
                    "payOS returned failed response: {RawJson}",
                    rawJson);

                throw new FailureException(
                    ErrorCodes.PayOsCreatePaymentFailed,
                    "payOS returned failed response",
                    new List<string> { rawJson });
            }

            var data = root.GetProperty("data");

            _logger.LogInformation(
                "payOS create payment succeeded: {RawJson}",
                rawJson);

            var paymentLinkId =
                data.GetProperty("paymentLinkId").GetString() ?? string.Empty;

            var checkoutUrl =
                data.GetProperty("checkoutUrl").GetString() ?? string.Empty;

            return new CreatePayOsPaymentResult
            {
                PaymentLinkId = paymentLinkId,
                CheckoutUrl = checkoutUrl,
                ExpiredAt = DateTimeOffset
                    .FromUnixTimeSeconds(expiredAtUnix)
                    .UtcDateTime
            };
        }

        public async Task<PayOsPaymentStatusResult> GetPaymentStatusAsync(long orderCode)
        {
            ValidateSettings();

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/v2/payment-requests/{orderCode}");

            httpRequest.Headers.Add("x-client-id", _settings.ClientId);
            httpRequest.Headers.Add("x-api-key", _settings.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest);
            var rawJson = await response.Content.ReadAsStringAsync();

            // Log the HTTP status and orderCode only — never the API keys.
            _logger.LogInformation(
                "payOS get payment status: orderCode={OrderCode} httpStatus={StatusCode}",
                orderCode,
                (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "payOS get payment status failed: orderCode={OrderCode} httpStatus={StatusCode}",
                    orderCode,
                    (int)response.StatusCode);

                throw new FailureException(
                    ErrorCodes.PayOsCreatePaymentFailed,
                    "Failed to query payOS payment status",
                    new List<string>
                    {
                        $"OrderCode: {orderCode}",
                        $"StatusCode: {(int)response.StatusCode}",
                        rawJson
                    });
            }

            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            var code = root.TryGetProperty("code", out var codeProp)
                ? codeProp.GetString() ?? string.Empty
                : string.Empty;

            var desc = root.TryGetProperty("desc", out var descProp)
                ? descProp.GetString() ?? string.Empty
                : string.Empty;

            var result = new PayOsPaymentStatusResult
            {
                Code = code,
                Desc = desc,
                OrderCode = orderCode,
                RawJson = rawJson
            };

            if (root.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Object)
            {
                if (data.TryGetProperty("status", out var statusProp))
                {
                    result.Status = (statusProp.GetString() ?? string.Empty)
                        .Trim()
                        .ToUpperInvariant();
                }

                if (data.TryGetProperty("orderCode", out var ocProp) &&
                    ocProp.ValueKind == JsonValueKind.Number &&
                    ocProp.TryGetInt64(out var oc))
                {
                    result.OrderCode = oc;
                }

                if (data.TryGetProperty("amount", out var amtProp) &&
                    amtProp.ValueKind == JsonValueKind.Number)
                {
                    result.Amount = amtProp.GetInt32();
                }

                if (data.TryGetProperty("amountPaid", out var amtPaidProp) &&
                    amtPaidProp.ValueKind == JsonValueKind.Number)
                {
                    result.AmountPaid = amtPaidProp.GetInt32();
                }
            }

            return result;
        }

        public bool VerifyWebhookSignature(
            object data,
            string signature)
        {
            // Fail closed: missing signature, missing checksum key, or a
            // payload that is not the nested webhook data object => reject.
            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_settings.ChecksumKey))
            {
                return false;
            }

            if (data is not JsonElement element ||
                element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var canonicalData = BuildCanonicalData(element);

            var expectedSignature = GenerateHmacSha256(
                canonicalData,
                _settings.ChecksumKey);

            var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
            var receivedBytes = Encoding.UTF8.GetBytes(
                signature.Trim().ToLowerInvariant());

            return CryptographicOperations.FixedTimeEquals(
                expectedBytes,
                receivedBytes);
        }

        // Reconstructs the payOS canonical string: data fields sorted by key
        // (ascending), joined as key=value&key2=value2. The signature field,
        // if echoed inside data, is excluded from the signed content.
        private static string BuildCanonicalData(JsonElement data)
        {
            var pairs = new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (var property in data.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        "signature",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                pairs[property.Name] = ConvertJsonValueToString(property.Value);
            }

            return string.Join("&", pairs.Select(p => $"{p.Key}={p.Value}"));
        }

        private static string ConvertJsonValueToString(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;
                case JsonValueKind.Number:
                    return element.GetRawText();
                case JsonValueKind.True:
                    return "true";
                case JsonValueKind.False:
                    return "false";
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return string.Empty;
                default:
                    // Nested object/array: use the raw JSON text.
                    return element.GetRawText();
            }
        }

        private static string GenerateHmacSha256(
            string data,
            string checksumKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(checksumKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA256(keyBytes);

            var hashBytes = hmac.ComputeHash(dataBytes);

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private void ValidateSettings()
        {
            var missingKeys = new List<string>();

            if (string.IsNullOrWhiteSpace(_settings.ClientId))
            {
                missingKeys.Add("PayOs__ClientId");
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                missingKeys.Add("PayOs__ApiKey");
            }

            if (string.IsNullOrWhiteSpace(_settings.ChecksumKey))
            {
                missingKeys.Add("PayOs__ChecksumKey");
            }

            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                missingKeys.Add("PayOs__BaseUrl");
            }

            if (string.IsNullOrWhiteSpace(_settings.ReturnUrl))
            {
                missingKeys.Add("PayOs__ReturnUrl");
            }

            if (string.IsNullOrWhiteSpace(_settings.CancelUrl))
            {
                missingKeys.Add("PayOs__CancelUrl");
            }

            if (_settings.PaymentLinkExpireMinutes <= 0)
            {
                missingKeys.Add("PayOs__PaymentLinkExpireMinutes");
            }

            if (missingKeys.Count == 0)
            {
                return;
            }

            throw new FailureException(
                ErrorCodes.PayOsCreatePaymentFailed,
                "payOS configuration is missing required values",
                missingKeys);
        }
    }
}
