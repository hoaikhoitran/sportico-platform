using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Infrastructure.Services.Payments;
using Xunit;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>
/// Verifies the PayOS Chi (Payout) request contract per official docs (POST /v1/payouts):
///
///   Signature:
///     - Five fields sorted alphabetically: amount, description, referenceId,
///       toAccountNumber, toBin  (no toAccountName, no category)
///     - Algorithm: HMAC-SHA256(canonicalString, ChecksumKey), lowercase hex
///     - Delivered as x-signature HTTP request header — NOT in the JSON body
///
///   Request body:
///     - referenceId, amount, description, toBin, toAccountNumber
///     - category: string array ["salary"] when configured; omitted when null
///     - NO signature field in body
///     - NO toAccountName field in body (response-only field)
/// </summary>
public class PayOsPayoutSignatureTests
{
    // ── PayOsPayoutSigner.BuildCanonicalString ────────────────────────────────

    [Fact]
    public void BuildCanonicalString_SortsFiveFieldsAlphabetically()
    {
        // amount < description < referenceId < toAccountNumber < toBin (Ordinal)
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
    public void BuildCanonicalString_DoesNotIncludeToAccountName()
    {
        var result = PayOsPayoutSigner.BuildCanonicalString(1, "d", "r", "a", "b");
        Assert.DoesNotContain("toAccountName", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accountName",   result, StringComparison.OrdinalIgnoreCase);
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

    // ── PayOsPayoutSigner.Compute ─────────────────────────────────────────────

    [Fact]
    public void Compute_ProducesLowercaseHexOf64Chars()
    {
        var sig = PayOsPayoutSigner.Compute("data", "key");
        Assert.Equal(64, sig.Length);
        Assert.Equal(sig, sig.ToLowerInvariant());
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

    // ── Signature placement: x-signature header, NOT body ────────────────────

    [Fact]
    public async Task CreatePayoutAsync_SignatureIsInXSignatureHeader()
    {
        HttpRequestMessage? capturedRequest = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedRequest = req;
            _ = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }));

        await svc.CreatePayoutAsync(MinimalRequest("wr-h"), idempotencyKey: "wr-h");

        Assert.True(
            capturedRequest!.Headers.TryGetValues("x-signature", out var vals),
            "x-signature header must be present");

        var sig = vals!.Single();
        Assert.Matches("^[0-9a-f]{64}$", sig);   // lowercase hex HMAC-SHA256
    }

    [Fact]
    public async Task CreatePayoutAsync_BodyDoesNotContainSignature()
    {
        string? capturedBody = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }));

        await svc.CreatePayoutAsync(MinimalRequest("wr-nosig"), idempotencyKey: "wr-nosig");

        var root = JsonDocument.Parse(capturedBody!).RootElement;
        Assert.False(
            root.TryGetProperty("signature", out _),
            "JSON body must NOT contain 'signature' — signature belongs in x-signature header");
    }

    [Fact]
    public async Task CreatePayoutAsync_BodyDoesNotContainToAccountName()
    {
        string? capturedBody = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }));

        await svc.CreatePayoutAsync(MinimalRequest("wr-noname"), idempotencyKey: "wr-noname");

        var root = JsonDocument.Parse(capturedBody!).RootElement;
        Assert.False(
            root.TryGetProperty("toAccountName", out _),
            "toAccountName is a response-only field — must NOT appear in the request body");
    }

    [Fact]
    public async Task CreatePayoutAsync_XSignatureMatchesExpectedHmac()
    {
        // The x-signature header value must equal HMAC-SHA256 of the 5-field canonical string.
        const string checksumKey = "test-checksum-key";
        HttpRequestMessage? capturedRequest = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedRequest = req;
            _ = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }), checksumKey: checksumKey);

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-42", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789"
            },
            idempotencyKey: "wr-42");

        var headerSig = capturedRequest!.Headers.GetValues("x-signature").Single();

        var canonical = PayOsPayoutSigner.BuildCanonicalString(
            100_000, "SPORTICO WD", "wr-42", "0123456789", "970418");
        var expected = PayOsPayoutSigner.Compute(canonical, checksumKey);

        Assert.Equal(expected, headerSig);
    }

    // ── Body field contract ───────────────────────────────────────────────────

    [Fact]
    public async Task CreatePayoutAsync_BodyContainsExactly5CoreFields_WhenNoCategory()
    {
        string? capturedBody = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }));

        await svc.CreatePayoutAsync(MinimalRequest("wr-5f"), idempotencyKey: "wr-5f");

        var root = JsonDocument.Parse(capturedBody!).RootElement;

        // Required fields
        Assert.True(root.TryGetProperty("referenceId", out _));
        Assert.True(root.TryGetProperty("amount", out _));
        Assert.True(root.TryGetProperty("description", out _));
        Assert.True(root.TryGetProperty("toBin", out _));
        Assert.True(root.TryGetProperty("toAccountNumber", out _));

        // Must NOT be present
        Assert.False(root.TryGetProperty("signature",     out _), "signature must be in header, not body");
        Assert.False(root.TryGetProperty("toAccountName", out _), "toAccountName is response-only");
        Assert.False(root.TryGetProperty("category",      out _), "category absent when null");
    }

    // ── Category: omit when null, send as string array when configured ────────

    [Fact]
    public async Task CreatePayoutAsync_CategoryOmitted_WhenNull()
    {
        string? capturedBody = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }));

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-nocat", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", Category = null
            }, idempotencyKey: "wr-nocat");

        var root = JsonDocument.Parse(capturedBody!).RootElement;
        Assert.False(root.TryGetProperty("category", out _),
            "category must be absent from body when null");
    }

    [Fact]
    public async Task CreatePayoutAsync_CategoryIsStringArray_WhenConfigured()
    {
        string? capturedBody = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }));

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-cat", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", Category = "salary"
            }, idempotencyKey: "wr-cat");

        var root = JsonDocument.Parse(capturedBody!).RootElement;
        Assert.True(root.TryGetProperty("category", out var catProp),
            "category must be in body when configured");

        // Must be a JSON array, not a plain string
        Assert.Equal(JsonValueKind.Array, catProp.ValueKind);
        var items = catProp.EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("salary", items[0].GetString());
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static PayOsCreatePayoutRequest MinimalRequest(string refId) =>
        new()
        {
            ReferenceId = refId, Amount = 100_000, Description = "SPORTICO WD",
            ToBin = "970418", ToAccountNumber = "0123456789"
        };

    private static PayOsPayoutService BuildService(
        CapturingHandler handler,
        string checksumKey = "ck",
        string clientId = "cid",
        string apiKey = "akey")
    {
        var client   = new HttpClient(handler) { BaseAddress = new Uri("https://api-merchant.payos.vn") };
        var settings = MsOptions.Options.Create(new PayOsPayoutSettings
        {
            ClientId = clientId, ApiKey = apiKey, ChecksumKey = checksumKey,
            BaseUrl  = "https://api-merchant.payos.vn"
        });
        return new PayOsPayoutService(client, settings, NullLogger<PayOsPayoutService>.Instance);
    }

    private static HttpResponseMessage OkPayoutResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"code":"00","desc":"ok","data":{"id":"po_1","state":"PROCESSING"}}""",
                System.Text.Encoding.UTF8, "application/json")
        };

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;
        public CapturingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
            => _send = send;
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => _send(request, cancellationToken);
    }
}
