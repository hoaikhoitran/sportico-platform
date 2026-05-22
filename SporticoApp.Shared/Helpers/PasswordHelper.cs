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

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            // Verify the password against the hashed version
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
