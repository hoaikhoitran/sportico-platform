using System.Collections.Generic;
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

        public bool VerifyWebhookSignature(
            object data,
            string signature)
        {
            return !string.IsNullOrWhiteSpace(signature);
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
