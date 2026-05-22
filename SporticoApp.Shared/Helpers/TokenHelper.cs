using System.Security.Cryptography;

namespace SporticoApp.Shared.Helpers
{
    public static class TokenHelper
    {
        public static RefreshToken GenerateRefreshToken()
        {
            var randomBytes = new byte[64];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }
    }
}
