namespace SporticoApp.Application.Options
{
    /// <summary>
    /// Google OAuth / Google Identity Services configuration.
    /// <para>
    /// Values are resolved with "GoogleAuth:*" taking priority and the pre-existing flat
    /// environment variables (GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, GOOGLE_CALLBACK_URL,
    /// FRONTEND_URL) as the fallback — see <c>GoogleAuthOptionsBinder</c> in the Api project.
    /// </para>
    /// Never log or serialize <see cref="ClientSecret"/>.
    /// </summary>
    public class GoogleAuthOptions
    {
        public const string SectionName = "GoogleAuth";

        /// <summary>Public value — safe to expose to the frontend.</summary>
        public string? ClientId { get; set; }

        /// <summary>SECRET. Backend only. Must never appear in logs, Swagger, docs or responses.</summary>
        public string? ClientSecret { get; set; }

        /// <summary>Absolute URL Google redirects back to, e.g. https://host/api/auth/google/callback.</summary>
        public string? CallbackUrl { get; set; }

        /// <summary>Absolute base URL of the frontend, used to build the post-login redirect.</summary>
        public string? FrontendUrl { get; set; }

        /// <summary>Lifetime of a one-time exchange code. Clamped to 30..300 seconds.</summary>
        public int ExchangeCodeLifetimeSeconds { get; set; } = 90;

        /// <summary>
        /// The ID-token flow (POST /api/auth/google) only needs the client id; the redirect flow
        /// additionally needs the secret and both URLs.
        /// </summary>
        public bool IsIdTokenFlowConfigured => !string.IsNullOrWhiteSpace(ClientId);

        public bool IsRedirectFlowConfigured =>
            !string.IsNullOrWhiteSpace(ClientId) &&
            !string.IsNullOrWhiteSpace(ClientSecret) &&
            !string.IsNullOrWhiteSpace(CallbackUrl) &&
            !string.IsNullOrWhiteSpace(FrontendUrl);

        /// <summary>
        /// Names (never values) of the configuration keys that are missing, so a 503 can say what
        /// to configure without ever revealing a secret.
        /// </summary>
        public List<string> MissingRedirectFlowKeys()
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(ClientId)) missing.Add("GOOGLE_CLIENT_ID");
            if (string.IsNullOrWhiteSpace(ClientSecret)) missing.Add("GOOGLE_CLIENT_SECRET");
            if (string.IsNullOrWhiteSpace(CallbackUrl)) missing.Add("GOOGLE_CALLBACK_URL");
            if (string.IsNullOrWhiteSpace(FrontendUrl)) missing.Add("FRONTEND_URL");
            return missing;
        }

        public int EffectiveExchangeCodeLifetimeSeconds =>
            ExchangeCodeLifetimeSeconds < 30 ? 30
            : ExchangeCodeLifetimeSeconds > 300 ? 300
            : ExchangeCodeLifetimeSeconds;
    }
}
