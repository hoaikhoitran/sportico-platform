using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SporticoApp.Infrastructure.Services.Payments
{
    /// <summary>
    /// Builds the PayOS Payout (Chi) x-signature exactly as the official PayOS payout SDK does
    /// (https://github.com/payOSHQ/payos-payout-demo-nodejs · lib/signature.js · createSignature).
    ///
    /// Algorithm (must match the SDK byte-for-byte, otherwise PayOS returns
    /// code=201 "Mã kiểm tra(signature) không hợp lệ"):
    ///   1. deepSortObj: sort the request body keys alphabetically (Ordinal / code-unit order).
    ///   2. For each value:
    ///        - array / object  -> compact JSON.stringify (e.g. ["salary"])
    ///        - null / undefined -> ""
    ///        - everything else  -> String(value)
    ///   3. Build "encodeURIComponent(key)=encodeURIComponent(value)" joined by "&".
    ///   4. HMAC-SHA256(queryString, ChecksumKey) -> lowercase hex.
    ///
    /// The whole body is signed (referenceId, amount, description, toBin, toAccountNumber, and
    /// category when present). The result is sent in the x-signature HTTP header, never in the body.
    /// </summary>
    internal static class PayOsPayoutSigner
    {
        // Matches JS JSON.stringify escaping for the relaxed set (does not \uXXXX-escape & < > etc.).
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Builds the canonical query string that is HMAC'd, from the exact request body that is sent.
        /// Keys are sorted alphabetically; keys and values are URL-encoded (encodeURIComponent);
        /// array/object values are compact-JSON-serialized first.
        /// </summary>
        internal static string BuildCanonicalString(IReadOnlyDictionary<string, object?> body)
        {
            var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in body)
            {
                sorted[kv.Key] = Stringify(kv.Value);
            }

            return string.Join(
                "&",
                sorted.Select(p => $"{EncodeUriComponent(p.Key)}={EncodeUriComponent(p.Value)}"));
        }

        /// <summary>
        /// Computes the PayOS payout x-signature over the exact request body, using the ChecksumKey.
        /// </summary>
        internal static string ComputeBodySignature(
            IReadOnlyDictionary<string, object?> body,
            string checksumKey)
            => Compute(BuildCanonicalString(body), checksumKey);

        /// <summary>HMAC-SHA256 of <paramref name="canonicalString"/> -> lowercase hex.</summary>
        internal static string Compute(string canonicalString, string checksumKey)
        {
            var keyBytes  = Encoding.UTF8.GetBytes(checksumKey);
            var dataBytes = Encoding.UTF8.GetBytes(canonicalString);

            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Stringifies a body value the same way the SDK does before encoding:
        /// strings pass through, numbers/bools use invariant String(value), arrays/objects become
        /// compact JSON, null becomes "".
        /// </summary>
        private static string Stringify(object? value) => value switch
        {
            null              => string.Empty,
            string s          => s,
            bool b            => b ? "true" : "false",
            int or long or short or byte or sbyte or uint or ulong or ushort
                or decimal or double or float
                              => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            _                 => JsonSerializer.Serialize(value, JsonOpts)
        };

        /// <summary>
        /// Exact .NET equivalent of JavaScript encodeURIComponent.
        /// Uri.EscapeDataString follows RFC 3986 and additionally escapes ! ' ( ) * which
        /// encodeURIComponent leaves literal — so we convert those five back.
        /// </summary>
        private static string EncodeUriComponent(string value)
        {
            var escaped = Uri.EscapeDataString(value);
            return escaped
                .Replace("%21", "!")
                .Replace("%27", "'")
                .Replace("%28", "(")
                .Replace("%29", ")")
                .Replace("%2A", "*");
        }
    }
}
