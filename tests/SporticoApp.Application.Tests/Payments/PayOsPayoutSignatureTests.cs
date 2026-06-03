using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Infrastructure.Services.Payments;
using Xunit;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>
/// Verifies the PayOS Chi (Payout) signature conventions:
///   - canonical string covers the six signed fields sorted alphabetically
///     (amount, description, referenceId, toAccountName [when present], toAccountNumber, toBin)
///   - category is excluded from the canonical string
///   - toAccountName is normalised (uppercase, diacritics stripped) before signing and before sending
///   - the computed signature lives in the JSON request body as "signature", NOT in a header
/// </summary>
public class PayOsPayoutSignatureTests
{
    // ── PayOsPayoutSigner.BuildCanonicalString ────────────────────────────────

    [Fact]
    public void BuildCanonicalString_WithToAccountName_SortsAllSixFieldsAlphabetically()
    {
        // Alphabetical (Ordinal): amount < description < referenceId < toAccountName < toAccountNumber < toBin
        var result = PayOsPayoutSigner.BuildCanonicalString(
            amount: 100_000,
            description: "SPORTICO WD",
            referenceId: "ref-abc",
            toAccountName: "NGUYEN VAN A",
            toAccountNumber: "0123456789",
            toBin: "970418");

        Assert.Equal(
            "amount=100000&description=SPORTICO WD&referenceId=ref-abc" +
            "&toAccountName=NGUYEN VAN A&toAccountNumber=0123456789&toBin=970418",
            result);
    }

    [Fact]
    public void BuildCanonicalString_WithoutToAccountName_SortsFiveFields()
    {
        var result = PayOsPayoutSigner.BuildCanonicalString(
            100_000, "SPORTICO WD", "ref-abc", null, "0123456789", "970418");

        Assert.Equal(
            "amount=100000&description=SPORTICO WD&referenceId=ref-abc&toAccountNumber=0123456789&toBin=970418",
            result);
    }

    [Fact]
    public void BuildCanonicalString_EmptyToAccountName_TreatedAsAbsent()
    {
        var withNull  = PayOsPayoutSigner.BuildCanonicalString(1, "d", "r", null,  "a", "b");
        var withEmpty = PayOsPayoutSigner.BuildCanonicalString(1, "d", "r", "",    "a", "b");
        Assert.Equal(withNull, withEmpty);
    }

    [Fact]
    public void BuildCanonicalString_DoesNotIncludeCategory()
    {
        var result = PayOsPayoutSigner.BuildCanonicalString(1, "d", "r", null, "a", "b");
        Assert.DoesNotContain("category", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCanonicalString_DoesNotIncludeSignatureItself()
    {
        var result = PayOsPayoutSigner.BuildCanonicalString(1, "d", "r", null, "a", "b");
        Assert.DoesNotContain("signature", result, StringComparison.OrdinalIgnoreCase);
    }

    // ── PayOsPayoutSigner.NormalizeAccountName ────────────────────────────────

    [Theory]
    [InlineData("NGUYEN VAN A",    "NGUYEN VAN A")]   // already normalised — no change
    [InlineData("coach name",       "COACH NAME")]    // lowercase → uppercase
    [InlineData("  coach  name  ",  "COACH NAME")]    // leading/trailing/internal whitespace
    [InlineData("Nguyễn Văn A",     "NGUYEN VAN A")]  // Vietnamese diacritics stripped
    [InlineData("Trần Thị B",       "TRAN THI B")]    // more Vietnamese diacritics
    [InlineData("Đỗ Minh C",        "DO MINH C")]     // Đ → D
    public void NormalizeAccountName_ProducesExpectedResult(string input, string expected)
    {
        Assert.Equal(expected, PayOsPayoutSigner.NormalizeAccountName(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeAccountName_NullOrWhitespace_ReturnsNull(string? input)
    {
        Assert.Null(PayOsPayoutSigner.NormalizeAccountName(input));
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

    // ── Signature placement: body field, not HTTP header ─────────────────────

    [Fact]
    public async Task CreatePayoutAsync_SignatureIsInBodyField_NotInHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedRequest = req;
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }));

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-001", Amount = 200_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", ToAccountName = "NGUYEN VAN A"
            },
            idempotencyKey: "wr-001");

        var root = JsonDocument.Parse(capturedBody!).RootElement;

        Assert.True(root.TryGetProperty("signature", out var sigProp),
            "Request body must contain 'signature' field");
        Assert.Matches("^[0-9a-f]{64}$", sigProp.GetString());

        Assert.False(capturedRequest!.Headers.Contains("x-signature"),
            "Request must NOT have x-signature header — signature belongs in the body");
    }

    [Fact]
    public async Task CreatePayoutAsync_SignatureMatchesExpectedHmac_IncludingNormalisedToAccountName()
    {
        const string checksumKey = "test-checksum-key";
        string? capturedBody = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }), checksumKey: checksumKey);

        // Send raw Vietnamese name — service must normalise it and sign the normalised form
        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-42", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", ToAccountName = "Nguyễn Văn A"
            },
            idempotencyKey: "wr-42");

        var bodySig = JsonDocument.Parse(capturedBody!).RootElement
            .GetProperty("signature").GetString()!;

        // Must sign the NORMALISED name "NGUYEN VAN A", not the raw "Nguyễn Văn A"
        var normalised = PayOsPayoutSigner.NormalizeAccountName("Nguyễn Văn A");
        var canonical  = PayOsPayoutSigner.BuildCanonicalString(
            100_000, "SPORTICO WD", "wr-42", normalised, "0123456789", "970418");
        var expected = PayOsPayoutSigner.Compute(canonical, checksumKey);

        Assert.Equal(expected, bodySig);
    }

    [Fact]
    public async Task CreatePayoutAsync_ToAccountNameInBody_IsNormalised()
    {
        // The body field must carry the normalised (uppercase, no diacritics) form
        string? capturedBody = null;

        var svc = BuildService(new CapturingHandler((req, ct) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }));

        await svc.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-norm", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", ToAccountName = "Nguyễn Văn A"
            },
            idempotencyKey: "wr-norm");

        var root = JsonDocument.Parse(capturedBody!).RootElement;
        Assert.True(root.TryGetProperty("toAccountName", out var nameProp));
        Assert.Equal("NGUYEN VAN A", nameProp.GetString()); // normalised, not raw
    }

    [Fact]
    public async Task CreatePayoutAsync_ToAccountName_AffectsSignature()
    {
        // Changing toAccountName must produce a different signature (proves it's in the canonical string)
        const string key = "ck";
        string? body1 = null, body2 = null;

        var svc1 = BuildService(new CapturingHandler((req, ct) =>
        {
            body1 = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }), checksumKey: key);

        await svc1.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-s", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", ToAccountName = "NGUYEN VAN A"
            }, idempotencyKey: "wr-s");

        var svc2 = BuildService(new CapturingHandler((req, ct) =>
        {
            body2 = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            return Task.FromResult(OkPayoutResponse());
        }), checksumKey: key);

        await svc2.CreatePayoutAsync(
            new PayOsCreatePayoutRequest
            {
                ReferenceId = "wr-s", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789", ToAccountName = "TRAN THI B" // different
            }, idempotencyKey: "wr-s");

        var sig1 = JsonDocument.Parse(body1!).RootElement.GetProperty("signature").GetString();
        var sig2 = JsonDocument.Parse(body2!).RootElement.GetProperty("signature").GetString();
        Assert.NotEqual(sig1, sig2); // toAccountName is signed — different name = different sig
    }

    // ── Category conditional inclusion ───────────────────────────────────────

    [Fact]
    public async Task CreatePayoutAsync_CategoryOmitted_WhenNotConfigured()
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
                ReferenceId = "wr-cat-none", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789",
                ToAccountName = "NGUYEN VAN A", Category = null
            }, idempotencyKey: "wr-cat-none");

        var root = JsonDocument.Parse(capturedBody!).RootElement;
        Assert.False(root.TryGetProperty("category", out _),
            "category must be absent when not configured — PayOS rejects unknown values");
    }

    [Fact]
    public async Task CreatePayoutAsync_CategoryIncluded_WhenExplicitlyConfigured()
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
                ReferenceId = "wr-cat-biz", Amount = 100_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "0123456789",
                ToAccountName = "NGUYEN VAN A", Category = "business"
            }, idempotencyKey: "wr-cat-biz");

        var root = JsonDocument.Parse(capturedBody!).RootElement;
        Assert.True(root.TryGetProperty("category", out var catProp));
        Assert.Equal("business", catProp.GetString());
    }

    // ── Full body field list ──────────────────────────────────────────────────

    [Fact]
    public async Task CreatePayoutAsync_BodyContainsAllRequiredFields()
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
                ReferenceId = "wr-99", Amount = 50_000, Description = "SPORTICO WD",
                ToBin = "970418", ToAccountNumber = "9876543210",
                ToAccountName = "TRAN THI B", Category = "salary"
            }, idempotencyKey: "wr-99");

        var root = JsonDocument.Parse(capturedBody!).RootElement;
        Assert.True(root.TryGetProperty("referenceId", out _));
        Assert.True(root.TryGetProperty("amount", out _));
        Assert.True(root.TryGetProperty("description", out _));
        Assert.True(root.TryGetProperty("toBin", out _));
        Assert.True(root.TryGetProperty("toAccountNumber", out _));
        Assert.True(root.TryGetProperty("toAccountName", out _));
        Assert.True(root.TryGetProperty("signature", out _));
        Assert.True(root.TryGetProperty("category", out _));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static PayOsPayoutService BuildService(
        CapturingHandler handler,
        string checksumKey = "ck",
        string clientId = "cid",
        string apiKey = "akey")
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api-merchant.payos.vn") };
        var settings = MsOptions.Options.Create(new PayOsPayoutSettings
        {
            ClientId = clientId, ApiKey = apiKey, ChecksumKey = checksumKey,
            BaseUrl = "https://api-merchant.payos.vn"
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
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _send(request, cancellationToken);
    }
}
