// PayOS Chi x-signature variant diagnostic tool.
//
// Sends 8 variants of the same /v1/payouts request to PayOS — each with an identical
// body but a different HMAC signing convention — to identify which canonical string
// produces a valid x-signature. Stops on the first non-201 response.
//
// ⚠  REAL MONEY WARNING ─────────────────────────────────────────────────────
//   If PayOS returns code=00 for any variant it WILL initiate a real bank
//   transfer to the configured account. Only run with the intended beneficiary
//   account and amount.
// ──────────────────────────────────────────────────────────────────────────
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
// Or add keys to the root .env — the tool loads it automatically.
// Never commit credentials. Never log ChecksumKey, ApiKey, ClientId, or signature value.

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

// ── Load .env from repo root if present ──────────────────────────────────────
var envFile = Path.Combine(FindRepoRoot() ?? ".", ".env");
if (File.Exists(envFile)) { DotNetEnv.Env.Load(envFile); Log($"[env] loaded {envFile}"); }

// ── Credentials & payout data ─────────────────────────────────────────────────
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

Log($"\n{'=',72}");
Log($"PayOS Chi x-signature Diagnostic  ·  run={runId}  amount={amount}  toBin={toBin}");
Log($"  toAccountNumber (masked) : {maskedAcct}");
Log($"  baseUrl                  : {baseUrl}");
Log($"  checksumKeyPresent       : {!string.IsNullOrEmpty(checksumKey)}");
Log($"  checksumKeyLength        : {checksumKey.Length}");
Log($"{'=',72}\n");

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

// ── Variant helper ────────────────────────────────────────────────────────────
// Each variant sends the SAME body but a DIFFERENT canonical string → HMAC.
// The body always contains exactly: referenceId, amount, description, toBin, toAccountNumber.
// The only change between variants is how we build the string that is HMAC'd for x-signature.

bool found = false;

async Task TryVariant(
    string label,
    string variantDesc,
    string refId,
    string idempotencyKeyValue,
    string canonicalString,
    string bodyDescription,
    int bodyAmount,
    bool differentRef = false)
{
    if (found) return;

    Log($"{'─',72}");
    Log($"Variant {label}: {variantDesc}");

    var sig = HmacHex(canonicalString, checksumKey);

    // Body is always the 5 documented fields — only x-signature changes
    var body = new Dictionary<string, object?>
    {
        ["referenceId"]     = refId,
        ["amount"]          = bodyAmount,
        ["description"]     = bodyDescription,
        ["toBin"]           = toBin,
        ["toAccountNumber"] = toAcctNum,
    };

    // Safe log — never log sig value or key
    var safeCanonical = canonicalString
        .Replace(toAcctNum, maskedAcct);

    Log($"  referenceId         : {refId}");
    Log($"  idempotencyKey      : {idempotencyKeyValue}");
    Log($"  refEqualsIdempotency: {refId == idempotencyKeyValue}");
    Log($"  canonicalStrSafe    : {safeCanonical}");
    Log($"  signatureLength     : {sig.Length}");
    Log($"  bodyFields          : [{string.Join(", ", body.Keys)}]");

    using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/payouts");
    req.Headers.Add("x-client-id",      clientId);
    req.Headers.Add("x-api-key",        apiKey);
    req.Headers.Add("x-idempotency-key", idempotencyKeyValue);
    req.Headers.Add("x-signature",      sig);
    req.Content = JsonContent.Create(body);

    HttpResponseMessage resp;
    string rawJson;
    try
    {
        resp    = await http.SendAsync(req);
        rawJson = await resp.Content.ReadAsStringAsync();
    }
    catch (Exception ex)
    {
        Log($"  !! HTTP error: {ex.Message}");
        return;
    }

    using var doc = JsonDocument.Parse(rawJson);
    var root      = doc.RootElement;
    var code      = root.TryGetProperty("code", out var cp) ? cp.GetString() : "?";
    var respDesc  = root.TryGetProperty("desc", out var dp) ? dp.GetString() : "?";

    Log($"  httpStatus  : {(int)resp.StatusCode}");
    Log($"  payosCode   : {code}");
    Log($"  payosDesc   : {respDesc}");
    Log($"  rawResponse : {rawJson}");

    if (code != "201")
    {
        Log($"\n★ FIRST NON-201 RESPONSE on Variant {label}");
        Log($"  code={code}  desc={respDesc}");
        Log($"  → Update production PayOsPayoutSigner to use this canonical string convention.");
        found = true;
    }
}

// ── Variant A — current production: sorted 5-field ──────────────────────────
// amount=<n>&description=<d>&referenceId=<r>&toAccountNumber=<a>&toBin=<b>
{
    var refId = $"{runId}-A";
    var canonical = Sorted(new()
    {
        ["amount"]          = amount.ToString(),
        ["description"]     = description,
        ["referenceId"]     = refId,
        ["toAccountNumber"] = toAcctNum,
        ["toBin"]           = toBin,
    });
    await TryVariant("A", "current: sorted 5-field canonical", refId, refId, canonical, description, amount);
}

// ── Variant B — sorted 5-field + x-idempotency-key included in canonical ────
{
    var refId = $"{runId}-B";
    var canonical = Sorted(new()
    {
        ["amount"]          = amount.ToString(),
        ["description"]     = description,
        ["idempotencyKey"]  = refId,        // same value as referenceId
        ["referenceId"]     = refId,
        ["toAccountNumber"] = toAcctNum,
        ["toBin"]           = toBin,
    });
    await TryVariant("B", "sorted 5-field + idempotencyKey in canonical", refId, refId, canonical, description, amount);
}

// ── Variant C — HMAC over minified JSON body string ─────────────────────────
// PayOS might sign the JSON directly rather than a key=value canonical string
{
    var refId = $"{runId}-C";
    var jsonBody = JsonSerializer.Serialize(new Dictionary<string, object?>
    {
        ["referenceId"]     = refId,
        ["amount"]          = amount,
        ["description"]     = description,
        ["toBin"]           = toBin,
        ["toAccountNumber"] = toAcctNum,
    });
    await TryVariant("C", "HMAC over minified JSON body", refId, refId, jsonBody, description, amount);
}

// ── Variant D — fields in body insertion order (not alphabetically sorted) ──
{
    var refId = $"{runId}-D";
    var canonical = $"referenceId={refId}&amount={amount}&description={description}&toBin={toBin}&toAccountNumber={toAcctNum}";
    await TryVariant("D", "body insertion order (not sorted)", refId, refId, canonical, description, amount);
}

// ── Variant E — URL-encoded values ──────────────────────────────────────────
// Some APIs use %20 for spaces in the canonical string
{
    var refId = $"{runId}-E";
    var canonical = Sorted(new()
    {
        ["amount"]          = amount.ToString(),
        ["description"]     = Uri.EscapeDataString(description),   // "SPORTICO%20WD"
        ["referenceId"]     = refId,
        ["toAccountNumber"] = toAcctNum,
        ["toBin"]           = toBin,
    });
    await TryVariant("E", "URL-encoded field values in canonical", refId, refId, canonical, description, amount);
}

// ── Variant F — description without space (SPORTICOWD) ──────────────────────
// Tests whether the space in "SPORTICO WD" causes PayOS to reject the signature
{
    var refId      = $"{runId}-F";
    var descNoSpc  = "SPORTICOWD";
    var canonical  = Sorted(new()
    {
        ["amount"]          = amount.ToString(),
        ["description"]     = descNoSpc,
        ["referenceId"]     = refId,
        ["toAccountNumber"] = toAcctNum,
        ["toBin"]           = toBin,
    });
    // Body also uses the no-space description so signature and body are consistent
    await TryVariant("F", "description=SPORTICOWD (no space) in body and canonical", refId, refId, canonical, descNoSpc, amount);
}

// ── Variant G — referenceId (body) ≠ idempotencyKey (header) ────────────────
// Tests if PayOS signs over idempotencyKey from the header rather than referenceId from body.
// Canonical is built with the body referenceId; idempotency-key header differs.
{
    var refId          = $"{runId}-G-ref";
    var idempotencyVal = $"{runId}-G-idempotency-different";
    var canonical      = Sorted(new()
    {
        ["amount"]          = amount.ToString(),
        ["description"]     = description,
        ["referenceId"]     = refId,    // signed using body referenceId
        ["toAccountNumber"] = toAcctNum,
        ["toBin"]           = toBin,
    });
    await TryVariant("G", "referenceId≠idempotencyKey: PayOS may sign over idempotency header",
        refId, idempotencyVal, canonical, description, amount);
}

// ── Variant H — referenceId = idempotencyKey = bare UUID (no retry suffix) ──
// Tests if a plain UUID referenceId (no "-retry-" suffix) behaves differently
{
    var baseRef   = $"{runId}-H";
    var canonical = Sorted(new()
    {
        ["amount"]          = amount.ToString(),
        ["description"]     = description,
        ["referenceId"]     = baseRef,
        ["toAccountNumber"] = toAcctNum,
        ["toBin"]           = toBin,
    });
    await TryVariant("H", "bare UUID referenceId=idempotencyKey (no -retry- suffix)",
        baseRef, baseRef, canonical, description, amount);
}

// ── Summary ───────────────────────────────────────────────────────────────────
Log($"\n{'=',72}");
if (!found)
{
    Log("✗ All variants returned code=201 (invalid signature).");
    Log("  The ChecksumKey is almost certainly wrong for this PayOS Chi channel.");
    Log("  Contact PayOS support with one referenceId from above and ask them to");
    Log("  verify the exact ClientId/ApiKey/ChecksumKey set and signature formula");
    Log("  for POST /v1/payouts on your merchant account.");
}
Log($"{'=',72}\n");

// ── Helpers ───────────────────────────────────────────────────────────────────

static string HmacHex(string data, string key)
{
    var keyBytes  = Encoding.UTF8.GetBytes(key);
    var dataBytes = Encoding.UTF8.GetBytes(data);
    using var hmac = new HMACSHA256(keyBytes);
    return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLowerInvariant();
}

static string Sorted(Dictionary<string, string> fields)
{
    var sorted = new SortedDictionary<string, string>(fields, StringComparer.Ordinal);
    return string.Join("&", sorted.Select(p => $"{p.Key}={p.Value}"));
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
        Console.Error.WriteLine($"FATAL: required env var {key} is not set.");
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
