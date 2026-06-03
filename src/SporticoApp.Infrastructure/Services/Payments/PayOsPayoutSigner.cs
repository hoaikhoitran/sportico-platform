using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SporticoApp.Infrastructure.Services.Payments
{
    /// <summary>
    /// Builds the PayOS Payout (Chi) canonical string and computes the HMAC-SHA256 signature.
    ///
    /// PayOS Chi signature spec:
    ///   - Fields signed: amount, description, referenceId, toAccountName (when present),
    ///     toAccountNumber, toBin  — sorted alphabetically (Ordinal)
    ///   - Fields excluded from signature: category, signature itself
    ///   - Format: key=value&amp;key=value... (no URL encoding)
    ///   - Algorithm: HMAC-SHA256, key = ChecksumKey, output = lowercase hex
    ///   - toAccountName must be normalised (uppercase, no diacritics) before signing
    ///
    /// The computed signature must be sent as the "signature" field in the JSON request body,
    /// NOT as an HTTP header.
    /// </summary>
    internal static class PayOsPayoutSigner
    {
        /// <summary>
        /// Builds the canonical string for the PayOS payout signature.
        /// <paramref name="toAccountName"/> is included when non-null/non-empty
        /// (its alphabetical position is after referenceId and before toAccountNumber).
        /// Pass the already-normalised value — see <see cref="NormalizeAccountName"/>.
        /// </summary>
        internal static string BuildCanonicalString(
            int amount,
            string description,
            string referenceId,
            string? toAccountName,
            string toAccountNumber,
            string toBin)
        {
            // SortedDictionary with Ordinal comparer produces stable alphabetical ordering:
            // amount < description < referenceId < toAccountName < toAccountNumber < toBin
            var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["amount"] = amount.ToString(),
                ["description"] = description,
                ["referenceId"] = referenceId,
                ["toAccountNumber"] = toAccountNumber,
                ["toBin"] = toBin
            };

            if (!string.IsNullOrEmpty(toAccountName))
            {
                fields["toAccountName"] = toAccountName;
            }

            return string.Join("&", fields.Select(p => $"{p.Key}={p.Value}"));
        }

        /// <summary>
        /// Normalises a bank account holder name for the PayOS Chi API.
        /// PayOS stores and validates names in the uppercase-Latin format used by the bank
        /// (i.e. as printed on the card / as returned by the bank inquiry API).
        ///
        /// Steps:
        ///   1. Decompose to Unicode NFD so base characters and combining marks are separate.
        ///   2. Strip combining diacritical marks (accents, Vietnamese tone marks, etc.).
        ///   3. Collapse internal whitespace runs to a single space.
        ///   4. Trim leading/trailing whitespace.
        ///   5. Convert to uppercase.
        ///
        /// Examples: "Nguyễn Văn A" → "NGUYEN VAN A", "coach name" → "COACH NAME".
        /// Returns null when the input is null or becomes empty after stripping.
        /// </summary>
        internal static string? NormalizeAccountName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // Pre-process characters that have no Unicode NFD canonical decomposition and
            // therefore would survive the combining-mark strip below unchanged.
            // Vietnamese banking systems store names in ASCII, so these must be mapped manually.
            // Only Đ/đ (U+0110/U+0111, D-with-stroke) lacks NFD decomposition in Vietnamese.
            // All other Vietnamese letters (e.g. Ơ U+01A0, Ư U+01AF) decompose correctly via NFD.
            var preprocessed = name.Replace("Đ", "D").Replace("đ", "d");

            // NFD decomposition separates e.g. 'ễ' (U+1EBF) into 'e' + combining marks
            var decomposed = preprocessed.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue; // strip accents / tone marks

                if (char.IsWhiteSpace(c))
                {
                    // Collapse consecutive spaces into a single space
                    if (sb.Length > 0 && sb[^1] != ' ')
                        sb.Append(' ');
                }
                else
                {
                    sb.Append(c);
                }
            }

            var result = sb.ToString().Trim().ToUpperInvariant();
            return result.Length == 0 ? null : result;
        }

        /// <summary>
        /// Computes HMAC-SHA256 of <paramref name="canonicalString"/> using
        /// <paramref name="checksumKey"/>. Returns lowercase hex — e.g. "a3f2...".
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
