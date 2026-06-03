using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SporticoApp.Infrastructure.Services.Payments
{
    /// <summary>
    /// Builds the PayOS Payout (Chi) canonical string and computes the HMAC-SHA256 signature.
    ///
    /// PayOS Chi signature spec:
    ///   - Fields included: amount, description, referenceId, toAccountNumber, toBin
    ///   - Fields excluded: category, signature itself, any optional fields not in this set
    ///   - Sorting: alphabetical by key (Ordinal)
    ///   - Format: key=value&amp;key=value... (no URL encoding, same as PayOS webhook convention)
    ///   - Algorithm: HMAC-SHA256, key = ChecksumKey, output = lowercase hex
    ///
    /// The computed signature must be sent as the "signature" field in the JSON request body,
    /// NOT as an HTTP header.
    /// </summary>
    internal static class PayOsPayoutSigner
    {
        /// <summary>
        /// Builds the canonical string for the PayOS payout signature.
        /// Only the five core fields are included — category is deliberately excluded.
        /// </summary>
        internal static string BuildCanonicalString(
            int amount,
            string description,
            string referenceId,
            string toAccountNumber,
            string toBin)
        {
            // SortedDictionary with Ordinal comparer ensures stable alphabetical ordering
            // regardless of locale: amount < description < referenceId < toAccountNumber < toBin
            var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["amount"] = amount.ToString(),
                ["description"] = description,
                ["referenceId"] = referenceId,
                ["toAccountNumber"] = toAccountNumber,
                ["toBin"] = toBin
            };

            return string.Join("&", fields.Select(p => $"{p.Key}={p.Value}"));
        }

        /// <summary>
        /// Computes HMAC-SHA256 of <paramref name="canonicalString"/> using <paramref name="checksumKey"/>.
        /// Returns lowercase hex — e.g. "a3f2...".
        /// </summary>
        internal static string Compute(string canonicalString, string checksumKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(checksumKey);
            var dataBytes = Encoding.UTF8.GetBytes(canonicalString);

            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
