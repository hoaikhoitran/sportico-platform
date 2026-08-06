using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Shared.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            // Implement a secure hashing algorithm, e.g., BCrypt
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifies a password against a stored hash. A null/empty/whitespace hash means the
        /// account has no local password (e.g. a Google-only account) and always returns false —
        /// BCrypt.Verify would otherwise throw, turning a normal failed login into a 500.
        /// A malformed hash is also treated as "no match" rather than propagating a salt-parsing
        /// exception to the caller.
        /// </summary>
        public static bool VerifyPassword(string? password, string? hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (SaltParseException)
            {
                return false;
            }
        }
    }
}
