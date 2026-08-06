using System;
using System.Security.Cryptography;
using System.Text;

namespace SporticoApp.Shared.Helpers
{
    /// <summary>
    /// One-way hash for client IP addresses so visitor-analytics storage never contains a raw IP.
    /// The pepper (salt) is mixed in so the hash cannot be reversed via a rainbow table even if it
    /// leaks; it is not an authentication secret.
    /// </summary>
    public static class IpHasher
    {
        public static string Hash(string? ipAddress, string salt)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes($"{salt}:{ipAddress.Trim()}");
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
