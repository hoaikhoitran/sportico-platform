using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Services.Payments
{
    /// <summary>
    /// Implements the PayOS Payout (Chi) API.
    ///
    /// Signature canonical string: request body fields sorted alphabetically,
    /// joined as key=value&amp;... (excluding category and any null fields),
    /// signed with HMAC-SHA256 using the ChecksumKey.
    ///
    /// Fields included in the signature: amount, description, referenceId,
    /// toAccountNumber, toBin — same convention as PayOS webhook verification.
    /// </summary>
    public class PayOsPayoutService : IPayOsPayoutService
    {
        private readonly HttpClient _httpClient;
        private readonly PayOsPayoutSettings _settings;
        private readonly ILogger<PayOsPayoutService> _logger;

        public PayOsPayoutService(
            HttpClient httpClient,
            IOptions<PayOsPayoutSettings> settings,
            ILogger<PayOsPayoutService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<PayOsPayoutBalanceResponse> GetBalanceAsync()
        {
            ValidatePayoutSettings();

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/v1/payouts-account/balance");

            AddAuthHeaders(httpRequest, null);

            var response = await _httpClient.SendAsync(httpRequest);
            var rawJson = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "PayOS payout balance response: {StatusCode}",
                (int)response.StatusCode);

            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            var code = root.TryGetProperty("code", out var codeProp)
                ? codeProp.GetString() ?? string.Empty
                : string.Empty;

            var desc = root.TryGetProperty("desc", out var descProp)
                ? descProp.GetString() ?? string.Empty
                : string.Empty;

            decimal balance = 0;
            string currency = "VND";

            if (root.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Object)
            {
                if (data.TryGetProperty("availableBalance", out var bal))
                {
                    balance = bal.ValueKind == JsonValueKind.Number
                        ? bal.GetDecimal()
                        : 0;
                }

                if (data.TryGetProperty("currency", out var curr))
                {
                    currency = curr.GetString() ?? "VND";
                }
            }

            return new PayOsPayoutBalanceResponse
            {
                Code = code,
                Desc = desc,
                AvailableBalance = balance,
                Currency = currency,
                RawJson = rawJson
            };
        }

        public async Task<PayOsCreatePayoutResponse> CreatePayoutAsync(
            PayOsCreatePayoutRequest request,
            string idempotencyKey)
        {
            ValidatePayoutSettings();
            ValidatePayoutRequest(request);

            // Build the EXACT request body first. The signature is computed over this same body
            // (deep-sorted, URL-encoded, arrays JSON-stringified) — matching the official PayOS
            // payout SDK (payos-payout-demo-nodejs/lib/signature.js). See docs/payos-evidence/.
            var body = new Dictionary<string, object?>
            {
                ["referenceId"]     = request.ReferenceId,
                ["amount"]          = request.Amount,
                ["description"]     = request.Description,
                ["toBin"]           = request.ToBin,
                ["toAccountNumber"] = request.ToAccountNumber,
            };

            // category is optional, merchant-account-specific, and sent as a string array per
            // PayOS Chi API spec. Omit entirely when not configured. When present it is part of
            // the body AND therefore part of the signature.
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                body["category"] = new[] { request.Category };
            }

            // PayOS Chi API: signature is HMAC-SHA256 over the URL-encoded, deep-sorted body and is
            // sent as the x-signature HTTP request header, NOT as a field in the JSON request body.
            var canonicalString = PayOsPayoutSigner.BuildCanonicalString(body);
            var signature       = PayOsPayoutSigner.Compute(canonicalString, _settings.ChecksumKey);

            // Safe canonical string for logging — account number masked. The canonical string is not
            // a secret (HMAC is one-way), but we mask the account to avoid exposure in log aggregators.
            var safeCanonical = canonicalString.Replace(
                request.ToAccountNumber,
                MaskAccountNumber(request.ToAccountNumber));

            // Pre-send diagnostics — enough to diagnose signature/schema rejections.
            // ClientId, ApiKey, ChecksumKey, and the raw signature value are NEVER logged.
            _logger.LogInformation(
                "PayOS Chi pre-send: referenceId={ReferenceId} amount={Amount} " +
                "description={Description} toBin={ToBin} maskedAccount={MaskedAccount} " +
                "categoryIncluded={CategoryIncluded} category={Category} " +
                "signatureLocation=header signatureScheme=encodeURIComponent bodyFields=[{BodyFields}] " +
                "canonicalStringSafe={CanonicalStringSafe} " +
                "signatureLength={SignatureLength} " +
                "checksumKeyPresent={ChecksumKeyPresent} checksumKeyLength={ChecksumKeyLength} " +
                "idempotencyKey={IdempotencyKey} referenceIdEqualsIdempotencyKey={RefEqualsKey}",
                request.ReferenceId,
                request.Amount,
                request.Description,
                request.ToBin,
                MaskAccountNumber(request.ToAccountNumber),
                body.ContainsKey("category"),
                request.Category,
                string.Join(", ", body.Keys),
                safeCanonical,
                signature.Length,
                !string.IsNullOrEmpty(_settings.ChecksumKey),
                _settings.ChecksumKey?.Length ?? 0,
                idempotencyKey,
                request.ReferenceId == idempotencyKey);

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "/v1/payouts");

            AddAuthHeaders(httpRequest, idempotencyKey);
            httpRequest.Headers.Add("x-signature", signature);
            httpRequest.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(httpRequest);
            var rawJson = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            var code = root.TryGetProperty("code", out var codeProp)
                ? codeProp.GetString() ?? string.Empty
                : string.Empty;

            var desc = root.TryGetProperty("desc", out var descProp)
                ? descProp.GetString() ?? string.Empty
                : string.Empty;

            // Post-response diagnostics: log code + desc so rejections are visible in logs.
            // rawJson is also logged — it contains no credentials (only payout metadata).
            _logger.LogInformation(
                "PayOS Chi response: referenceId={ReferenceId} amount={Amount} " +
                "httpStatus={HttpStatus} payosCode={PayOsCode} payosDesc={PayOsDesc} rawResponse={RawResponse}",
                request.ReferenceId,
                request.Amount,
                (int)response.StatusCode,
                code,
                desc,
                rawJson);

            PayOsPayoutData? payoutData = null;

            if (root.TryGetProperty("data", out var dataProp) &&
                dataProp.ValueKind == JsonValueKind.Object)
            {
                payoutData = ParsePayoutData(dataProp);
            }

            return new PayOsCreatePayoutResponse
            {
                Code = code,
                Desc = desc,
                Data = payoutData,
                RawJson = rawJson
            };
        }

        public async Task<PayOsPayoutDetailResponse> GetPayoutDetailAsync(string payoutId)
        {
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/v1/payouts/{Uri.EscapeDataString(payoutId)}");

            AddAuthHeaders(httpRequest, null);

            var response = await _httpClient.SendAsync(httpRequest);
            var rawJson = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "PayOS get payout detail: payoutId={PayoutId} status={StatusCode}",
                payoutId,
                (int)response.StatusCode);

            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            var code = root.TryGetProperty("code", out var codeProp)
                ? codeProp.GetString() ?? string.Empty
                : string.Empty;

            var desc = root.TryGetProperty("desc", out var descProp)
                ? descProp.GetString() ?? string.Empty
                : string.Empty;

            PayOsPayoutData? payoutData = null;

            if (root.TryGetProperty("data", out var dataProp) &&
                dataProp.ValueKind == JsonValueKind.Object)
            {
                payoutData = ParsePayoutData(dataProp);
            }

            return new PayOsPayoutDetailResponse
            {
                Code = code,
                Desc = desc,
                Data = payoutData,
                RawJson = rawJson
            };
        }

        /// <summary>Returns a masked account number safe to write to logs, e.g. "01****89".</summary>
        private static string MaskAccountNumber(string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length <= 4)
                return "****";

            return accountNumber[..2]
                + new string('*', accountNumber.Length - 4)
                + accountNumber[^2..];
        }

        private void AddAuthHeaders(HttpRequestMessage request, string? idempotencyKey)
        {
            request.Headers.Add("x-client-id", _settings.ClientId);
            request.Headers.Add("x-api-key", _settings.ApiKey);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.Headers.Add("x-idempotency-key", idempotencyKey);
            }
        }

        private static PayOsPayoutData ParsePayoutData(JsonElement data)
        {
            var result = new PayOsPayoutData();

            if (data.TryGetProperty("id", out var id))
                result.Id = id.GetString() ?? string.Empty;

            if (data.TryGetProperty("referenceId", out var refId))
                result.ReferenceId = refId.GetString() ?? string.Empty;

            if (data.TryGetProperty("state", out var state))
                result.State = state.GetString() ?? string.Empty;
            else if (data.TryGetProperty("status", out var status))
                result.State = status.GetString() ?? string.Empty;

            // Batch payouts (id like "batch_…") may nest the outcome inside a transactions array
            // rather than exposing a top-level state/status. Fall back to the first transaction's
            // state/status so the withdrawal can still be reconciled to a terminal outcome.
            if (string.IsNullOrWhiteSpace(result.State) &&
                data.TryGetProperty("transactions", out var txns) &&
                txns.ValueKind == JsonValueKind.Array)
            {
                foreach (var txn in txns.EnumerateArray())
                {
                    if (txn.ValueKind != JsonValueKind.Object)
                        continue;

                    if (txn.TryGetProperty("state", out var txnState))
                        result.State = txnState.GetString() ?? string.Empty;
                    else if (txn.TryGetProperty("status", out var txnStatus))
                        result.State = txnStatus.GetString() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(result.State))
                        break;
                }
            }

            if (data.TryGetProperty("toBin", out var toBin))
                result.ToBin = toBin.GetString();

            if (data.TryGetProperty("toAccountNumber", out var toAcct))
                result.ToAccountNumber = toAcct.GetString();

            if (data.TryGetProperty("toAccountName", out var toName))
                result.ToAccountName = toName.GetString();

            if (data.TryGetProperty("amount", out var amt) &&
                amt.ValueKind == JsonValueKind.Number)
                result.Amount = amt.GetInt32();

            if (data.TryGetProperty("description", out var desc))
                result.Description = desc.GetString();

            if (data.TryGetProperty("category", out var cat))
                result.Category = cat.GetString();

            return result;
        }

        /// <summary>
        /// Fails loudly (rather than calling PayOS with empty credentials) when the dedicated
        /// payout-channel credentials are not configured. Lists exactly which keys are missing.
        /// </summary>
        private void ValidatePayoutSettings()
        {
            var missingKeys = new List<string>();

            if (string.IsNullOrWhiteSpace(_settings.ClientId))
            {
                missingKeys.Add("PayOsPayout:ClientId");
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                missingKeys.Add("PayOsPayout:ApiKey");
            }

            if (string.IsNullOrWhiteSpace(_settings.ChecksumKey))
            {
                missingKeys.Add("PayOsPayout:ChecksumKey");
            }

            if (missingKeys.Count == 0)
            {
                return;
            }

            throw new FailureException(
                ErrorCodes.PayOsPayoutFailed,
                "PayOS payout (Chi) configuration is missing required values. " +
                "Configure the dedicated payout channel credentials via PayOsPayout__* " +
                "(Azure App Settings / environment variables) — do not reuse the inbound PayOs__* keys.",
                missingKeys);
        }

        private static void ValidatePayoutRequest(PayOsCreatePayoutRequest request)
        {
            var details = new List<string>();

            if (request.Amount <= 0)
                details.Add("Amount must be greater than zero");

            if (string.IsNullOrWhiteSpace(request.ReferenceId))
                details.Add("ReferenceId is required");

            // PayOS Chi requires an exact 6-digit bank BIN (no spaces, no dashes).
            if (string.IsNullOrWhiteSpace(request.ToBin))
            {
                details.Add("ToBin is required");
            }
            else if (request.ToBin.Length != 6 || !request.ToBin.All(char.IsAsciiDigit))
            {
                details.Add($"ToBin must be exactly 6 ASCII digits (got '{request.ToBin}')");
            }

            // Bank account numbers in Vietnam are digits only; no hyphens or spaces allowed.
            if (string.IsNullOrWhiteSpace(request.ToAccountNumber))
            {
                details.Add("ToAccountNumber is required");
            }
            else if (!request.ToAccountNumber.Trim().All(char.IsAsciiDigit))
            {
                details.Add($"ToAccountNumber must contain digits only (got length={request.ToAccountNumber.Length})");
            }

            if (details.Count > 0)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid PayOS payout request",
                    details);
            }
        }

    }
}
