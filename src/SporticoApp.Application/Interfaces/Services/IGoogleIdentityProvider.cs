using SporticoApp.Application.DTOs.Auth;

namespace SporticoApp.Application.Interfaces.Services
{
    /// <summary>
    /// Verifies a Google ID token. Implemented in Infrastructure with the official
    /// <c>Google.Apis.Auth</c> library so the Application layer never depends on a Google SDK.
    /// </summary>
    public interface IGoogleIdentityProvider
    {
        /// <summary>
        /// Fully validates the token — RS256 signature against Google's published keys, issuer,
        /// audience == the configured client id, and expiry — and projects the verified claims.
        /// Implementations must throw <c>UnauthorizedException(ErrorCodes.GoogleInvalidToken)</c>
        /// for any invalid token, and must never trust an unverified payload.
        /// </summary>
        Task<GoogleIdentity> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
    }
}
