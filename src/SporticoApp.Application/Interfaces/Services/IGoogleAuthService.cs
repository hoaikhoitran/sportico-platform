using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IGoogleAuthService
    {
        /// <summary>Flow A: verify a Google ID token, resolve/link/create the user, issue Sportico tokens.</summary>
        Task<Result<LoginResponse>> LoginWithIdTokenAsync(GoogleIdTokenLoginRequest request);

        /// <summary>
        /// Flow B step 1: resolve/link/create the user from an already-verified external identity
        /// (produced by the Google OAuth handler) and return a one-time exchange code in PLAINTEXT.
        /// The plaintext is returned here and nowhere else — only its SHA-256 hash is persisted.
        /// </summary>
        Task<string> CreateExchangeCodeForIdentityAsync(GoogleIdentity identity);

        /// <summary>Flow B step 2: atomically consume the code and issue Sportico tokens.</summary>
        Task<Result<LoginResponse>> ExchangeCodeAsync(GoogleExchangeCodeRequest request);
    }
}
