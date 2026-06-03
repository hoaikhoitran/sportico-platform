using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Infrastructure.Services.Payments;
using Xunit;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>
/// Verifies the PayOS Chi (Payout) signature convention:
///   - canonical string covers the five core fields sorted alphabetically
///   - category is excluded from the canonical string
///   - the computed signature lives in the JSON request body as "signature",
///     NOT in an x-signature HTTP header
/// </summary>
public class PayOsPayoutSignatureTests
{
    // ── PayOsPayoutSigner unit tests ─────────────────────────────────────────

    [Fact]
    public void BuildCanonicalString_FieldsAreSortedAlphabetically()
    {
        // Alphabetical order: amount < description < referenceId < toAccountNumber < toBin
        var result = PayOsPayoutSigner.BuildCanonicalString(
            amount: 100_000,
            description: "SPORTICO WD",
            referenceId: "ref-abc",
            toAccountNumber: "0123456789",
            toBin: "970418");

        Assert.Equal(
            "amount=100000&description=SPORTICO WD&referenceId=ref-abc&toAccountNumber=0123456789&toBin=970418",
            result);
    }

    [Fact]
    public void BuildCanonicalString_DoesNotIncludeCategory()
    {
        var result = PayOsPayoutSigner.BuildCanonicalString(1, "d", "r", "a", "b");

        Assert.DoesNotContain("category", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCanonicalString_DoesNotIncludeSignatureItself()
    {
        var result = PayOsPayoutSigner.BuildCanonicalString(1, "d", "r", "a", "b");

        Assert.DoesNotContain("signature", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compute_ProducesLowercaseHexOf64Chars()
    {
        // HMAC-SHA256 is always 32 bytes = 64 hex chars
        var sig = PayOsPayoutSigner.Compute("data", "key");

        Assert.Equal(64, sig.Length);
        Assert.Equal(sig, sig.ToLowerInvariant()); // must be lowercase
        Assert.Matches("^[0-9a-f]{64}$", sig);
    }

    [Fact]
    public void Compute_SameInputProducesSameOutput()
    {
        var a = PayOsPayoutSigner.Compute("amount=100&description=test", "mykey");
        var b = PayOsPayoutSigner.Compute("amount=100&description=test", "mykey");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Compute_DifferentKeyProducesDifferentOutput()
    {
        var a = PayOsPayoutSigner.Compute("same-data", "key-a");
        var b = PayOsPayoutSigner.Compute("same-data", "key-b");

        Assert.NotEqual(a, b);
    }

    // ── Signature placement: body field, not HTTP header ────────────────────

    [Fact]
    public async Task CreatePayoutAsync_SignatureIsInBodyField_NotInHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new CapturingHandler((req, ct) =>
        {
            capturedRequest = req;
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        });

        var svc = BuildService(handler);

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-001",
                Amount = 200_000,
                Description = "SPORTICO WD",
                ToBin = "970418",
                ToAccountNumber = "0123456789",
                Category = "salary"
            },
            idempotencyKey: "wr-001");

        Assert.NotNull(capturedBody);

        var doc = JsonDocument.Parse(capturedBody!);
        var root = doc.RootElement;

        // signature must be present in the body
        Assert.True(
            root.TryGetProperty("signature", out var sigProp),
            "Request body must contain 'signature' field");

        var sigValue = sigProp.GetString();
        Assert.False(string.IsNullOrWhiteSpace(sigValue), "signature must not be empty");
        Assert.Matches("^[0-9a-f]{64}$", sigValue); // lowercase hex HMAC-SHA256

        // signature must NOT be sent as an HTTP header
        Assert.False(
            capturedRequest!.Headers.Contains("x-signature"),
            "Request must NOT have x-signature header — signature belongs in the body");
    }

    [Fact]
    public async Task CreatePayoutAsync_SignatureMatchesExpectedHmac()
    {
        const string checksumKey = "test-checksum-key";
        string? capturedBody = null;

        var handler = new CapturingHandler((req, ct) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        });

        var svc = BuildService(handler, checksumKey: checksumKey);

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-42",
                Amount = 100_000,
                Description = "SPORTICO WD",
                ToBin = "970418",
                ToAccountNumber = "0123456789"
            },
            idempotencyKey: "wr-42");

        var doc = JsonDocument.Parse(capturedBody!);
        var bodySig = doc.RootElement.GetProperty("signature").GetString()!;

        // Recompute what the signature should be
        var canonical = PayOsPayoutSigner.BuildCanonicalString(100_000, "SPORTICO WD", "wr-42", "0123456789", "970418");
        var expected = PayOsPayoutSigner.Compute(canonical, checksumKey);

        Assert.Equal(expected, bodySig);
    }

    [Fact]
    public async Task CreatePayoutAsync_BodyContainsAllRequiredFields()
    {
        string? capturedBody = null;

        var handler = new CapturingHandler((req, ct) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        });

        var svc = BuildService(handler);

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-99",
                Amount = 50_000,
                Description = "SPORTICO WD",
                ToBin = "970418",
                ToAccountNumber = "9876543210",
                Category = "salary"
            },
            idempotencyKey: "wr-99");

        var root = JsonDocument.Parse(capturedBody!).RootElement;

        // All payload fields the PayOS Chi API requires must be present
        Assert.True(root.TryGetProperty("referenceId", out _));
        Assert.True(root.TryGetProperty("amount", out _));
        Assert.True(root.TryGetProperty("description", out _));
        Assert.True(root.TryGetProperty("toBin", out _));
        Assert.True(root.TryGetProperty("toAccountNumber", out _));
        Assert.True(root.TryGetProperty("signature", out _));
        Assert.True(root.TryGetProperty("category", out _)); // optional but was provided
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static PayOsPayoutService BuildService(
        CapturingHandler handler,
        string checksumKey = "ck",
        string clientId = "cid",
        string apiKey = "akey")
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api-merchant.payos.vn") };
        var settings = MsOptions.Options.Create(new PayOsPayoutSettings
        {
            ClientId = clientId,
            ApiKey = apiKey,
            ChecksumKey = checksumKey,
            BaseUrl = "https://api-merchant.payos.vn"
        });
        return new PayOsPayoutService(client, settings, NullLogger<PayOsPayoutService>.Instance);
    }

    private static HttpResponseMessage OkPayoutResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"code":"00","desc":"ok","data":{"id":"po_1","state":"PROCESSING"}}""",
                System.Text.Encoding.UTF8,
                "application/json")
        };

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

        public CapturingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
            => _send = send;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _send(request, cancellationToken);
    }
}
