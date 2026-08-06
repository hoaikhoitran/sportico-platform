using SporticoApp.Application.Options;

namespace SporticoApp.Api.Configuration;

/// <summary>
/// Turns the configured absolute <c>GOOGLE_CALLBACK_URL</c> into the pieces ASP.NET Core needs,
/// and refuses values that would produce an unsafe redirect.
/// </summary>
public static class GoogleCallbackUrlResolver
{
    /// <summary>Used when the callback URL is absent so the app can still start.</summary>
    public const string DefaultCallbackPath = "/api/auth/google/callback";

    /// <summary>
    /// Extracts the path Google will call back on. Returns <see cref="DefaultCallbackPath"/> when
    /// nothing valid is configured — the Google endpoints then answer 503, but every other endpoint
    /// keeps working.
    /// </summary>
    public static string ResolveCallbackPath(GoogleAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CallbackUrl))
        {
            return DefaultCallbackPath;
        }

        if (!Uri.TryCreate(options.CallbackUrl, UriKind.Absolute, out var uri))
        {
            return DefaultCallbackPath;
        }

        return string.IsNullOrWhiteSpace(uri.AbsolutePath) || uri.AbsolutePath == "/"
            ? DefaultCallbackPath
            : uri.AbsolutePath;
    }

    /// <summary>
    /// The callback URL must be absolute, and outside Development it must be HTTPS — an http
    /// callback would put the OAuth exchange on the wire in clear text.
    /// </summary>
    public static bool IsCallbackUrlValid(GoogleAuthOptions options, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(options.CallbackUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(options.CallbackUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        // Allow plain http on loopback for local development only.
        return isDevelopment && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
    }

    /// <summary>
    /// Builds the post-login frontend URL. The path is fixed and the host always comes from the
    /// configured FRONTEND_URL, so a caller can never steer the redirect at an external site.
    /// </summary>
    public static string BuildFrontendCallbackUrl(
        GoogleAuthOptions options,
        string? code,
        string? error)
    {
        var baseUrl = (options.FrontendUrl ?? string.Empty).TrimEnd('/');
        var query = code != null
            ? "?code=" + Uri.EscapeDataString(code)
            : "?error=" + Uri.EscapeDataString(error ?? "AUTH_GOOGLE_LOGIN_FAILED");

        return $"{baseUrl}/auth/google/callback{query}";
    }
}
