// PayOS Chi x-signature variant diagnostic tool.
//
// Sends several variants of the same /v1/payouts request to PayOS — each with the same body
// but a different x-signature signing convention — to confirm which canonical-string formula
// PayOS accepts. Variant A is the corrected production formula (encodeURIComponent over the
// whole body, matching the official PayOS payout SDK); the rest are fallback hypotheses.
//
// ⚠  REAL MONEY WARNING ─────────────────────────────────────────────────────
//   If PayOS returns code=00 / a success state it WILL initiate a real bank
//   transfer to the configured account. Only run with the intended beneficiary
//   account and a small amount.
// ──────────────────────────────────────────────────────────────────────────
//
// RESULT CLASSIFICATION (important — do not misread a 403 as success):
//   code=00 / success-like   -> ACCEPTED variant found, stop.
//   code=403 (IP not allowed)-> INCONCLUSIVE, stop. The signature was never even checked;
//                               run this from a whitelisted host (e.g. Azure), not locally.
//   code=201 (invalid sig)   -> signature rejected, continue to the next variant.
//   code=20  (bad data)      -> schema/data validation, continue.
//
// Usage (PowerShell — from repo root):
//   $env:PAYOS_PAYOUT_CLIENTID        = "..."   # PayOsPayout__ClientId from Azure
//   $env:PAYOS_PAYOUT_APIKEY          = "..."   # PayOsPayout__ApiKey from Azure
//   $env:PAYOS_PAYOUT_CHECKSUMKEY     = "..."   # PayOsPayout__ChecksumKey from Azure
//   $env:PAYOS_PAYOUT_TOBIN           = "970422"
//   $env:PAYOS_PAYOUT_TOACCOUNTNUMBER = "0983131727"
//   $env:PAYOS_PAYOUT_AMOUNT          = "10000"
//   dotnet run --project tools/PayOsSignatureDiagnostic
//
// Process environment variables take precedence over the repo .env (which only fills gaps).
// Never commit credentials. Never log ChecksumKey, ApiKey, ClientId, or the signature value.

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

// ── Load .env WITHOUT clobbering real process env vars (process env wins) ─────
var repoRoot = FindRepoRoot();
var envFile  = Path.Combine(repoRoot ?? ".", ".env");
if (File.Exists(envFile))
{
    DotNetEnv.Env.Load(envFile, new DotNetEnv.LoadOptions(clobberExistingVars: false));
    Log($"[env] loaded {envFile} (process env takes precedence; .env only fills gaps)");
}
else
{
    Log("[env] no .env file found; using process environment only");
}

// ── Credentials & payout data (report source safely, never values) ───────────
var clientId    = Require("PAYOS_PAYOUT_CLIENTID");
var apiKey      = Require("PAYOS_PAYOUT_APIKEY");
var checksumKey = Require("PAYOS_PAYOUT_CHECKSUMKEY");
var toBin       = Require("PAYOS_PAYOUT_TOBIN");
var toAcctNum   = Require("PAYOS_PAYOUT_TOACCOUNTNUMBER").Trim();
var amount      = int.Parse(Require("PAYOS_PAYOUT_AMOUNT"));
var baseUrl     = Environment.GetEnvironmentVariable("PAYOS_PAYOUT_BASEURL")
                  ?? "https://api-merchant.payos.vn";

var maskedAcct  = MaskAccount(toAcctNum);
var runId       = DateTime.UtcNow.ToString("yyyyMMddHHmm");
var description = "SPORTICO WD";

Log($"\n{new string('=', 72)}");
Log($"PayOS Chi x-signature Diagnostic  ·  run={runId}  amount={amount}  toBin={toBin}");
Log($"  toAccountNumber (masked) : {maskedAcct}");
Log($"  baseUrl                  : {baseUrl}");
Log($"  checksumKeyPresent       : {!string.IsNullOrEmpty(checksumKey)}");
Log($"  checksumKeyLength        : {checksumKey.Length}");
Log($"{new string('=', 72)}\n");

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

bool accepted     = false;   // a variant returned success → stop, we found it
bool inconclusive = false;   // 403 IP block → stop, cannot diagnose from here

// Each variant: a body dict + a function that builds the canonical string to HMAC.
// Variant A is the corrected production formula. The body sent is always the documented schema.

// ── Variant A — corrected production: encodeURIComponent over the whole body ─
await TryVariant("A", "CORRECTED PRODUCTION: encodeURIComponent over whole sorted body",
    BodyOf($"{runId}-A"), b => CanonicalEncoded(b));

// ── Variant B — corrected formula but with category included ─────────────────
{
    var body = BodyOf($"{runId}-B");
    body["category"] = new[] { "salary" };
    await TryVariant("B", "encodeURIComponent whole body + category=[\"salary\"]",
        body, b => CanonicalEncoded(b));
}

// ── Variant C — encodeURIComponent + idempotencyKey folded into canonical ────
{
    var body = BodyOf($"{runId}-C");
    await TryVariant("C", "encodeURIComponent + idempotencyKey in canonical",
        body, b =>
        {
            var withKey = new Dictionary<string, object?>(b) { ["idempotencyKey"] = b["referenceId"] };
            return CanonicalEncoded(withKey);
        });
}

// ── Variant D — OLD broken formula: raw key=value, NO url-encoding ───────────
// Expected to FAIL (code=201). Confirms the url-encoding was the bug.
await TryVariant("D", "OLD broken: raw key=value, no url-encoding (expected to fail)",
    BodyOf($"{runId}-D"), b => CanonicalRaw(b));

// ── Variant E — HMAC over minified JSON body ─────────────────────────────────
await TryVariant("E", "HMAC over minified JSON body",
    BodyOf($"{runId}-E"),
    b => JsonSerializer.Serialize(SortedClone(b),
        new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

// ── Variant F — description without space (SPORTICOWD) ───────────────────────
{
    var body = BodyOf($"{runId}-F");
    body["description"] = "SPORTICOWD";
    await TryVariant("F", "description=SPORTICOWD (no space) + encodeURIComponent",
        body, b => CanonicalEncoded(b));
}

// ── Variant G — referenceId (body) ≠ x-idempotency-key (header) ──────────────
{
    var body = BodyOf($"{runId}-G");
    await TryVariant("G", "referenceId ≠ idempotencyKey header (encodeURIComponent)",
        body, b => CanonicalEncoded(b), idempotencyOverride: $"{runId}-G-idem-diff");
}

// ── Variant H — bare referenceId == idempotencyKey, no suffix ───────────────
await TryVariant("H", "bare referenceId=idempotencyKey, encodeURIComponent",
    BodyOf($"{runId}-H"), b => CanonicalEncoded(b));

// ── Summary ──────────────────────────────────────────────────────────────────
Log($"\n{new string('=', 72)}");
if (inconclusive)
{
    Log("⚠ INCONCLUSIVE: PayOS returned code=403 (IP not allowed) — the signature was never");
    Log("  validated. This machine's IP is not whitelisted for the PayOS Chi channel.");
    Log("  Re-run from a whitelisted host (e.g. the Azure App Service) to test the signature.");
    Log("  DO NOT treat 403 as a passing or failing signature result.");
}
else if (accepted)
{
    Log("✓ A variant was ACCEPTED (success/processing). See the ★ ACCEPTED line above.");
}
else
{
    Log("✗ No variant was accepted; all reachable variants returned code=201 (invalid signature).");
    Log("  Variant A is byte-for-byte identical to the official PayOS payout SDK, so if even A");
    Log("  is rejected with code=201, the ChecksumKey is wrong for this ClientId/ApiKey pair,");
    Log("  or this merchant account's Chi/Payout channel is not provisioned.");
    Log("  Contact PayOS support with one referenceId above; do NOT keep guessing canonical strings.");
}
Log($"{new string('=', 72)}\n");

// ── Variant runner ─────────────────────────────────────────────────────────────
async Task TryVariant(
    string label,
    string variantDesc,
    Dictionary<string, object?> body,
    Func<Dictionary<string, object?>, string> buildCanonical,
    string? idempotencyOverride = null)
{
    if (accepted || inconclusive) return;

    var refId          = (string)body["referenceId"]!;
    var idempotencyKey = idempotencyOverride ?? refId;

    Log($"{new string('-', 72)}");
    Log($"Variant {label}: {variantDesc}");

    var canonical = buildCanonical(body);
    var sig       = HmacHex(canonical, checksumKey);
    var safeCanon = canonical.Replace(toAcctNum, maskedAcct);

    Log($"  referenceId         : {refId}");
    Log($"  idempotencyKey      : {idempotencyKey}");
    Log($"  refEqualsIdempotency: {refId == idempotencyKey}");
    Log($"  bodyFields          : [{string.Join(", ", body.Keys)}]");
    Log($"  canonicalStrSafe    : {safeCanon}");
    Log($"  signatureLength     : {sig.Length}");

    using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/payouts");
    req.Headers.Add("x-client-id",       clientId);
    req.Headers.Add("x-api-key",         apiKey);
    req.Headers.Add("x-idempotency-key", idempotencyKey);
    req.Headers.Add("x-signature",       sig);
    req.Content = JsonContent.Create(body);

    string rawJson;
    int httpStatus;
    try
    {
        var resp   = await http.SendAsync(req);
        httpStatus = (int)resp.StatusCode;
        rawJson    = await resp.Content.ReadAsStringAsync();
    }
    catch (Exception ex)
    {
        Log($"  !! HTTP error: {ex.Message}");
        return;
    }

    string code = "?", respDesc = "?";
    try
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        code     = root.TryGetProperty("code", out var cp) ? cp.GetString() ?? "?" : "?";
        respDesc = root.TryGetProperty("desc", out var dp) ? dp.GetString() ?? "?" : "?";
    }
    catch { /* non-JSON body — log raw below */ }

    Log($"  httpStatus  : {httpStatus}");
    Log($"  payosCode   : {code}");
    Log($"  payosDesc   : {respDesc}");
    Log($"  rawResponse : {rawJson}");

    // ── Classification ──────────────────────────────────────────────────────
    if (code is "403" || respDesc.Contains("IP", StringComparison.OrdinalIgnoreCase))
    {
        Log($"\n⚠ Variant {label}: code=403 IP NOT ALLOWED → INCONCLUSIVE. Stopping.");
        inconclusive = true;
        return;
    }

    if (code is "00" || code is "200")
    {
        Log($"\n★ ACCEPTED on Variant {label}: code={code} desc={respDesc}");
        Log($"  This canonical-string convention is correct for production.");
        accepted = true;
        return;
    }

    if (code is "201")
    {
        Log($"  → code=201 invalid signature; trying next variant.");
        return;
    }

    if (code is "20")
    {
        Log($"  → code=20 schema/data validation; trying next variant.");
        return;
    }

    Log($"  → unrecognised code={code}; trying next variant.");
}

// ── Canonical builders (mirror production PayOsPayoutSigner) ──────────────────

// encodeURIComponent over sorted body (the corrected production formula).
static string CanonicalEncoded(Dictionary<string, object?> body)
{
    var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
    foreach (var kv in body) sorted[kv.Key] = Stringify(kv.Value);
    return string.Join("&", sorted.Select(p => $"{Enc(p.Key)}={Enc(p.Value)}"));
}

// Raw key=value with NO url-encoding (the OLD broken formula).
static string CanonicalRaw(Dictionary<string, object?> body)
{
    var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
    foreach (var kv in body) sorted[kv.Key] = Stringify(kv.Value);
    return string.Join("&", sorted.Select(p => $"{p.Key}={p.Value}"));
}

static Dictionary<string, object?> SortedClone(Dictionary<string, object?> body)
{
    var sorted = new SortedDictionary<string, object?>(body, StringComparer.Ordinal);
    return new Dictionary<string, object?>(sorted);
}

Dictionary<string, object?> BodyOf(string refId) => new()
{
    ["referenceId"]     = refId,
    ["amount"]          = amount,
    ["description"]     = description,
    ["toBin"]           = toBin,
    ["toAccountNumber"] = toAcctNum,
};

// ── Low-level helpers ──────────────────────────────────────────────────────────

static string Stringify(object? value) => value switch
{
    null     => string.Empty,
    string s => s,
    bool b   => b ? "true" : "false",
    int or long or short or decimal or double or float
             => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
    _        => JsonSerializer.Serialize(value,
                    new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }),
};

// Exact .NET equivalent of encodeURIComponent.
static string Enc(string value)
{
    var escaped = Uri.EscapeDataString(value);
    return escaped.Replace("%21", "!").Replace("%27", "'")
                  .Replace("%28", "(").Replace("%29", ")").Replace("%2A", "*");
}

static string HmacHex(string data, string key)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
    return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
}

static string MaskAccount(string? acct)
{
    if (string.IsNullOrWhiteSpace(acct) || acct.Length <= 4) return "****";
    return acct[..2] + new string('*', acct.Length - 4) + acct[^2..];
}

static void Log(string msg) => Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {msg}");

static string Require(string key)
{
    var val = Environment.GetEnvironmentVariable(key);
    if (string.IsNullOrWhiteSpace(val))
    {
        Console.Error.WriteLine($"FATAL: required env var {key} is not set (process env or .env).");
        Environment.Exit(1);
    }
    return val!;
}

static string? FindRepoRoot()
{
    var dir = Directory.GetCurrentDirectory();
    for (var i = 0; i < 8; i++)
    {
        if (File.Exists(Path.Combine(dir, "SporticoApp.Api.sln"))) return dir;
        var parent = Directory.GetParent(dir)?.FullName;
        if (parent == null) break;
        dir = parent;
    }
    return null;
}
