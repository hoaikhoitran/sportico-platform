using System;
using System.Security.Cryptography;

namespace SporticoApp.Shared.Helpers
{
    /// <summary>
    /// Generates cryptographically strong, URL-safe tokens (e.g. for password reset
    /// and email verification links).
    /// </summary>
    public static class SecureTokenGenerator
    {
        public static string Generate(int byteLength = 32)
        {
            if (byteLength < 16)
            {
                byteLength = 16;
            }

            var bytes = RandomNumberGenerator.GetBytes(byteLength);

            // URL-safe Base64 (RFC 4648) without padding.
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
