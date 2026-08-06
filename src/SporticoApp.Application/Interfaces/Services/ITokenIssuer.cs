using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Services
{
    /// <summary>
    /// The single place Sportico access + refresh tokens are minted. Password login, Google
    /// ID-token login and Google exchange-code login all go through this, so there is exactly one
    /// implementation of "what a Sportico session is" — no parallel session mechanism.
    /// </summary>
    public interface ITokenIssuer
    {
        /// <summary>
        /// Generates an access token, rotates the user's refresh token, persists both, and returns
        /// the login response. The caller must pass a user loaded WITH roles, otherwise the JWT
        /// would be issued without role claims.
        /// </summary>
        Task<LoginResponse> IssueAsync(User userWithRoles);
    }
}
