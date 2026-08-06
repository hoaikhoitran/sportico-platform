using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Services
{
    /// <summary>
    /// Verifies Google ID tokens with the official <c>Google.Apis.Auth</c> library.
    /// <para>
    /// <see cref="GoogleJsonWebSignature.ValidateAsync"/> checks the RS256 signature against
    /// Google's published JWKS (cached and rotated by the library), the issuer, the expiry, and —
    /// because <see cref="GoogleJsonWebSignature.ValidationSettings.Audience"/> is set — that the
    /// token was minted for THIS client id. Never decode the payload manually; an unverified JWT
    /// body is attacker-controlled.
    /// </para>
    /// </summary>
    public class GoogleIdentityProvider : IGoogleIdentityProvider
    {
        private readonly GoogleAuthOptions _options;
        private readonly ILogger<GoogleIdentityProvider> _logger;

        public GoogleIdentityProvider(
            IOptions<GoogleAuthOptions> options,
            ILogger<GoogleIdentityProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<GoogleIdentity> VerifyIdTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ClientId))
            {
                // Name the key, never a value.
                throw new ServiceUnavailableException(
                    ErrorCodes.GoogleConfigurationMissing,
                    "Google sign-in is not configured on this environment.",
                    new List<string> { "GOOGLE_CLIENT_ID" });
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        // Audience must equal our client id, otherwise a token issued for a
                        // different app could be replayed against Sportico.
                        Audience = new[] { _options.ClientId }
                    });
            }
            catch (InvalidJwtException ex)
            {
                // Log the reason for operators; return a generic message to the client so token
                // validation internals are never disclosed. The token itself is never logged.
                _logger.LogWarning("Google ID token validation failed: {Reason}", ex.Message);

                throw new UnauthorizedException(
                    ErrorCodes.GoogleInvalidToken,
                    "Google authentication failed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Google ID token validation could not be completed: {ExceptionType}",
                    ex.GetType().FullName);

                throw new UnauthorizedException(
                    ErrorCodes.GoogleInvalidToken,
                    "Google authentication failed");
            }

            // Defence in depth: the library already enforces the issuer, but assert it explicitly
            // so a future settings change cannot silently widen what we accept.
            if (payload.Issuer != "accounts.google.com" && payload.Issuer != "https://accounts.google.com")
            {
                throw new UnauthorizedException(
                    ErrorCodes.GoogleInvalidToken,
                    "Google authentication failed");
            }

            return new GoogleIdentity
            {
                Subject = payload.Subject ?? string.Empty,
                Email = payload.Email ?? string.Empty,
                // EmailVerified is bool? on the payload; absent means NOT verified.
                EmailVerified = payload.EmailVerified == true,
                FullName = payload.Name,
                AvatarUrl = payload.Picture
            };
        }
    }
}
