using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Infrastructure.Services.Payments;
using Xunit;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>
/// Verifies the PayOS Chi (Payout) x-signature exactly matches the official PayOS payout SDK
/// (payos-payout-demo-nodejs/lib/signature.js — see docs/payos-evidence/):
///
///   - whole request body is signed (referenceId, amount, description, toBin,
///     toAccountNumber, and category when present)
///   - keys sorted alphabetically (Ordinal)
///   - keys AND values URL-encoded via encodeURIComponent (space -> %20)
///   - array values JSON-stringified before encoding (["salary"] -> %5B%22salary%22%5D)
///   - HMAC-SHA256 over that query string, lowercase hex
///   - delivered as the x-signature HTTP header, NEVER in the JSON body
/// </summary>
public class PayOsPayoutSignatureTests
{
    // ── BuildCanonicalString: URL-encoding (the regression that caused code=201) ──

    [Fact]
    public void BuildCanonicalString_UrlEncodesSpaceInDescription()
    {
        // "SPORTICO WD" MUST become "SPORTICO%20WD" — the raw space was the code=201 bug.
        var canonical = PayOsPayoutSigner.BuildCanonicalString(Body(
            referenceId: "ref-abc", amount: 100_000, description: "SPORTICO WD",
            toBin: "970418", toAccountNumber: "0123456789"));

        Assert.Contains("description=SPORTICO%20WD", canonical);
        Assert.DoesNotContain("description=SPORTICO WD", canonical); // raw space must be gone
    }

    [Fact]
    public void BuildCanonicalString_MatchesOfficialSdkGoldenVector()
    {
        // Golden vector mirroring payos-payout-demo-nodejs/index.js (category ["salary","hoa"]).
        var canonical = PayOsPayoutSigner.BuildCanonicalString(new Dictionary<string, object?>
        {
            ["referenceId"]     = "payout_123",
            ["amount"]          = 2000,
            ["description"]     = "payout",
            ["toBin"]           = "970422",
            ["toAccountNumber"] = "0973592402",
            ["category"]        = new[] { "salary", "hoa" },
        });

        Assert.Equal(
            "amount=2000" +
            "&category=%5B%22salary%22%2C%22hoa%22%5D" +   // JSON.stringify then encodeURIComponent
            "&description=payout" +
            "&referenceId=payout_123" +                    // underscore is NOT encoded
            "&toAccountNumber=0973592402" +
            "&toBin=970422",
            canonical);
    }

    [Fact]
    public void BuildCanonicalString_SortsKeysAlphabetically()
    {
        var canonical = PayOsPayoutSigner.BuildCanonicalString(Body(
            referenceId: "r", amount: 1, description: "d",
            toBin: "b", toAccountNumber: "a"));

        // amount < description < referenceId < toAccountNumber < toBin
        Assert.Equal("amount=1&description=d&referenceId=r&toAccountNumber=a&toBin=b", canonical);
    }

    [Fact]
    public void BuildCanonicalString_CategoryEncodedAsJsonArray_WhenPresent()
    {
        var canonical = PayOsPayoutSigner.BuildCanonicalString(new Dictionary<string, object?>
        {
            ["referenceId"]     = "r", ["amount"] = 1, ["description"] = "d",
            ["toBin"]           = "b", ["toAccountNumber"] = "a",
            ["category"]        = new[] { "salary" },
        });

        Assert.Contains("category=%5B%22salary%22%5D", canonical); // ["salary"] url-encoded
    }

    [Fact]
    public void BuildCanonicalString_OmitsCategory_WhenNotInBody()
    {
        var canonical = PayOsPayoutSigner.BuildCanonicalString(Body(
            referenceId: "r", amount: 1, description: "d", toBin: "b", toAccountNumber: "a"));

        Assert.DoesNotContain("category", canonical, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCanonicalString_AmountSerializedAsPlainNumber()
    {
        var canonical = PayOsPayoutSigner.BuildCanonicalString(Body(
            referenceId: "r", amount: 47_600, description: "d", toBin: "b", toAccountNumber: "a"));

        Assert.Contains("amount=47600", canonical); // not "47600.0", not quoted
    }

    // ── Compute ───────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_ProducesLowercaseHexOf64Chars()
    {
        var sig = PayOsPayoutSigner.Compute("data", "key");
        Assert.Equal(64, sig.Length);
        Assert.Equal(sig, sig.ToLowerInvariant());
        Assert.Matches("^[0-9a-f]{64}$", sig);
    }

    [Fact]
    public void Compute_DifferentKeyProducesDifferentOutput()
    {
        Assert.NotEqual(
            PayOsPayoutSigner.Compute("same-data", "key-a"),
            PayOsPayoutSigner.Compute("same-data", "key-b"));
    }

    // ── Signature placement: x-signature header, NOT body ────────────────────

    [Fact]
    public async Task CreatePayoutAsync_SignatureIsInXSignatureHeader()
    {
        HttpRequestMessage? captured = null;
        var svc = BuildService(Capture(req => captured = req));

        await svc.CreatePayoutAsync(MinimalRequest("wr-h"), idempotencyKey: "wr-h");

        Assert.True(captured!.Headers.TryGetValues("x-signature", out var vals),
            "x-signature header must be present");
        Assert.Matches("^[0-9a-f]{64}$", vals!.Single());
    }

    [Fact]
    public async Task CreatePayoutAsync_IdempotencyKeyHeaderPresent()
    {
        HttpRequestMessage? captured = null;
        var svc = BuildService(Capture(req => captured = req));

        await svc.CreatePayoutAsync(MinimalRequest("wr-i"), idempotencyKey: "idem-123");

        Assert.True(captured!.Headers.TryGetValues("x-idempotency-key", out var vals));
        Assert.Equal("idem-123", vals!.Single());
    }

    [Fact]
    public async Task CreatePayoutAsync_BodyDoesNotContainSignature()
    {
        string? body = null;
        var svc = BuildService(Capture(captureBody: b => body = b));

        await svc.CreatePayoutAsync(MinimalRequest("wr-nosig"), idempotencyKey: "wr-nosig");

        var root = JsonDocument.Parse(body!).RootElement;
        Assert.False(root.TryGetProperty("signature", out _),
            "signature belongs in x-signature header, not the body");
    }

    [Fact]
    public async Task CreatePayoutAsync_BodyDoesNotContainToAccountName()
    {
        string? body = null;
        var svc = BuildService(Capture(captureBody: b => body = b));

        await svc.CreatePayoutAsync(MinimalRequest("wr-noname"), idempotencyKey: "wr-noname");

        var root = JsonDocument.Parse(body!).RootElement;
        Assert.False(root.TryGetProperty("toAccountName", out _),
            "toAccountName is a response-only field — never in the request body");
    }

    [Fact]
    public async Task CreatePayoutAsync_XSignatureMatchesUrlEncodedBodyHmac()
    {
        const string checksumKey = "test-checksum-key";
        HttpRequestMessage? captured = null;
        var svc = BuildService(Capture(req => captured = req), checksumKey: checksumKey);

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-42", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789"
            },
            idempotencyKey: "wr-42");

        var headerSig = captured!.Headers.GetValues("x-signature").Single();

        // Recompute over the same body using the (URL-encoding) signer.
        var expected = PayOsPayoutSigner.ComputeBodySignature(Body(
            referenceId: "wr-42", amount: 100_000, description: "SPORTICO WD",
            toBin: "970418", toAccountNumber: "0123456789"), checksumKey);

        Assert.Equal(expected, headerSig);
    }

    [Fact]
    public async Task CreatePayoutAsync_CategorySignedWhenConfigured()
    {
        const string checksumKey = "ck";
        HttpRequestMessage? captured = null;
        var svc = BuildService(Capture(req => captured = req), checksumKey: checksumKey);

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-c", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", Category = "salary"
            },
            idempotencyKey: "wr-c");

        var headerSig = captured!.Headers.GetValues("x-signature").Single();

        // Expected signature must cover category as ["salary"].
        var expected = PayOsPayoutSigner.ComputeBodySignature(new Dictionary<string, object?>
        {
            ["referenceId"] = "wr-c", ["amount"] = 100_000, ["description"] = "SPORTICO WD",
            ["toBin"] = "970418", ["toAccountNumber"] = "0123456789",
            ["category"] = new[] { "salary" },
        }, checksumKey);

        Assert.Equal(expected, headerSig);
    }

    // ── Body field contract ───────────────────────────────────────────────────

    [Fact]
    public async Task CreatePayoutAsync_BodyContainsExactly5CoreFields_WhenNoCategory()
    {
        string? body = null;
        var svc = BuildService(Capture(captureBody: b => body = b));

        await svc.CreatePayoutAsync(MinimalRequest("wr-5f"), idempotencyKey: "wr-5f");

        var root = JsonDocument.Parse(body!).RootElement;
        Assert.True(root.TryGetProperty("referenceId", out _));
        Assert.True(root.TryGetProperty("amount", out _));
        Assert.True(root.TryGetProperty("description", out _));
        Assert.True(root.TryGetProperty("toBin", out _));
        Assert.True(root.TryGetProperty("toAccountNumber", out _));
        Assert.False(root.TryGetProperty("signature", out _));
        Assert.False(root.TryGetProperty("toAccountName", out _));
        Assert.False(root.TryGetProperty("category", out _));
    }

    [Fact]
    public async Task CreatePayoutAsync_CategoryOmitted_WhenNull()
    {
        string? body = null;
        var svc = BuildService(Capture(captureBody: b => body = b));

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-nocat", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", Category = null
            }, idempotencyKey: "wr-nocat");

        var root = JsonDocument.Parse(body!).RootElement;
        Assert.False(root.TryGetProperty("category", out _));
    }

    [Fact]
    public async Task CreatePayoutAsync_CategoryIsStringArray_WhenConfigured()
    {
        string? body = null;
        var svc = BuildService(Capture(captureBody: b => body = b));

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-cat", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", Category = "salary"
            }, idempotencyKey: "wr-cat");

        var root = JsonDocument.Parse(body!).RootElement;
        Assert.True(root.TryGetProperty("category", out var catProp));
        Assert.Equal(JsonValueKind.Array, catProp.ValueKind);
        Assert.Equal("salary", catProp.EnumerateArray().Single().GetString());
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static Dictionary<string, object?> Body(
        string referenceId, int amount, string description, string toBin, string toAccountNumber)
        => new()
        {
            ["referenceId"]     = referenceId,
            ["amount"]          = amount,
            ["description"]     = description,
            ["toBin"]           = toBin,
            ["toAccountNumber"] = toAccountNumber,
        };

    private static PayOsCreatePayoutRequest MinimalRequest(string refId) =>
        new()
        {
            ReferenceId = refId, Amount = 100_000, Description = "SPORTICO WD",
            ToBin = "970418", ToAccountNumber = "0123456789"
        };

    private static PayOsPayoutService BuildService(
        CapturingHandler handler, string checksumKey = "ck")
    {
        var client   = new HttpClient(handler) { BaseAddress = new Uri("https://api-merchant.payos.vn") };
        var settings = MsOptions.Options.Create(new PayOsPayoutSettings
        {
            ClientId = "cid", ApiKey = "akey", ChecksumKey = checksumKey,
            BaseUrl  = "https://api-merchant.payos.vn"
        });
        return new PayOsPayoutService(client, settings, NullLogger<PayOsPayoutService>.Instance);
    }

    private static CapturingHandler Capture(
        Action<HttpRequestMessage>? captureReq = null,
        Action<string>? captureBody = null)
        => new((req, ct) =>
        {
            captureReq?.Invoke(req);
            var b = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            captureBody?.Invoke(b);
            return Task.FromResult(OkPayoutResponse());
        });

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
