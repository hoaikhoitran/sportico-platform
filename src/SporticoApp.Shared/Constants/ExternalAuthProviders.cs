namespace SporticoApp.Shared.Constants
{
    /// <summary>
    /// Value stored in <c>user_external_logins.provider</c>. Kept lowercase and stable — it is part
    /// of the (provider, provider_subject) unique key, so changing a value here orphans existing rows.
    /// </summary>
    public static class ExternalAuthProviders
    {
        public const string Google = "google";
    }
}
