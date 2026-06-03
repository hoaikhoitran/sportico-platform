using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SporticoApp.Infrastructure.Services.Payments
{
    /// <summary>
    /// Builds the PayOS Payout (Chi) canonical string and computes the HMAC-SHA256 signature.
    ///
    /// PayOS Chi signature spec (POST /v1/payouts):
    ///   - Fields signed: amount, description, referenceId, toAccountNumber, toBin
    ///   - Fields excluded: category, toAccountName (response-only), signature itself
    ///   - Sorting: alphabetical by key (Ordinal)
    ///   - Format: key=value&amp;key=value... (no URL encoding)
    ///   - Algorithm: HMAC-SHA256, key = ChecksumKey, output = lowercase hex
    ///
    /// The computed signature is sent as the x-signature HTTP request header,
    /// NOT as a field in the JSON request body.
    /// </summary>
    internal static class PayOsPayoutSigner
    {
        /// <summary>
        /// Builds the canonical string for the PayOS payout HMAC-SHA256 signature.
        /// Exactly five fields, sorted alphabetically (Ordinal):
        /// amount, description, referenceId, toAccountNumber, toBin.
        /// </summary>
        internal static string BuildCanonicalString(
            int amount,
            string description,
            string referenceId,
            string toAccountNumber,
            string toBin)
        {
            var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["amount"]          = amount.ToString(),
                ["description"]     = description,
                ["referenceId"]     = referenceId,
                ["toAccountNumber"] = toAccountNumber,
                ["toBin"]           = toBin
            };

            return string.Join("&", fields.Select(p => $"{p.Key}={p.Value}"));
        }

        /// <summary>
        /// Computes HMAC-SHA256 of <paramref name="canonicalString"/> using
        /// <paramref name="checksumKey"/>. Returns lowercase hex — e.g. "a3f2...".
        /// </summary>
        internal static string Compute(string canonicalString, string checksumKey)
        {
            var keyBytes  = Encoding.UTF8.GetBytes(checksumKey);
            var dataBytes = Encoding.UTF8.GetBytes(canonicalString);

            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
