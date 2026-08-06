namespace SporticoApp.Application.DTOs.Auth
{
    /// <summary>Body of POST /api/auth/google (Google Identity Services flow).</summary>
    public class GoogleIdTokenLoginRequest
    {
        /// <summary>
        /// The Google **ID token** (JWT) from Google Identity Services — the `credential` field of
        /// CredentialResponse. Not an OAuth access token.
        /// </summary>
        public string IdToken { get; set; } = string.Empty;
    }

    /// <summary>Body of POST /api/auth/google/exchange (browser-redirect flow).</summary>
    public class GoogleExchangeCodeRequest
    {
        /// <summary>The one-time code the backend put on the frontend callback URL.</summary>
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Provider-agnostic identity produced by <c>IGoogleIdentityProvider</c> AFTER the token
    /// signature, issuer, audience and expiry have been verified. The Application layer only ever
    /// sees this — never a raw Google token or a Google SDK type.
    /// </summary>
    public class GoogleIdentity
    {
        /// <summary>Google's immutable "sub" claim. The only safe stable identifier.</summary>
        public string Subject { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool EmailVerified { get; set; }

        public string? FullName { get; set; }

        public string? AvatarUrl { get; set; }
    }
}
