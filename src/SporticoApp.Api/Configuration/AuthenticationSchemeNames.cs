namespace SporticoApp.Api.Controllers;

/// <summary>
/// Authentication scheme names. JWT Bearer stays the default for every <c>[Authorize]</c> API
/// endpoint; the external cookie exists only for the few milliseconds between Google's callback
/// and <c>/api/auth/google/complete</c>.
/// </summary>
public static class AuthenticationSchemeNames
{
    /// <summary>Temporary, short-lived cookie holding the external principal mid-handshake.</summary>
    public const string ExternalCookie = "Sportico.External";
}
