using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Services
{
    /// <summary>
    /// Extracted verbatim from the original AuthService.LoginAsync token-issuance block so every
    /// login path produces an identical session: same JWT claims, same one-refresh-token-per-user
    /// rotation, same expiry source.
    /// </summary>
    public class TokenIssuer : ITokenIssuer
    {
        private readonly IUserRepository _userRepo;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;

        public TokenIssuer(
            IUserRepository userRepo,
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService)
        {
            _userRepo = userRepo;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
        }

        public async Task<LoginResponse> IssueAsync(User userWithRoles)
        {
            var accessToken = _jwtService.GenerateAccessToken(userWithRoles);
            var refreshToken = _refreshTokenService.GenerateRefreshToken();

            userWithRoles.RefreshToken = refreshToken;
            userWithRoles.RefreshTokenExpiresAt = DateTime.UtcNow.Add(
                _refreshTokenService.GetRefreshTokenLifetime());

            await _userRepo.UpdateAsync(userWithRoles);

            return new LoginResponse
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken,
                ExpiresAt = accessToken.ExpiresAt
            };
        }
    }
}
