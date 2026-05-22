using Microsoft.Extensions.Configuration;
using SporticoApp.Application.Interfaces.Services;
using System;
using System.Security.Cryptography;

namespace SporticoApp.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IConfiguration _config;

        public RefreshTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }

        public TimeSpan GetRefreshTokenLifetime()
        {
            var daysValue = _config["JWT:RefreshTokenExpirationDays"];
            if (!int.TryParse(daysValue, out var days) || days <= 0)
            {
                days = 30;
            }

            return TimeSpan.FromDays(days);
        }
    }
}
