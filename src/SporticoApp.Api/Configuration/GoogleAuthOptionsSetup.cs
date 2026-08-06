using Microsoft.Extensions.Options;
using SporticoApp.Application.Options;

namespace SporticoApp.Api.Configuration;

/// <summary>
/// Binds <see cref="GoogleAuthOptions"/> from two sources, in priority order:
/// <list type="number">
///   <item>the .NET-convention <c>GoogleAuth:*</c> section (or <c>GoogleAuth__*</c> env vars);</item>
///   <item>the flat environment variables this deployment already uses —
///         <c>GOOGLE_CLIENT_ID</c>, <c>GOOGLE_CLIENT_SECRET</c>, <c>GOOGLE_CALLBACK_URL</c>,
///         <c>FRONTEND_URL</c>.</item>
/// </list>
/// Values are read through <see cref="IConfiguration"/> only and are never logged.
/// </summary>
public sealed class GoogleAuthOptionsSetup : IConfigureOptions<GoogleAuthOptions>
{
    private readonly IConfiguration _configuration;

    public GoogleAuthOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(GoogleAuthOptions options)
    {
        // Start from the structured section (may be all-empty placeholders in appsettings.json).
        _configuration.GetSection(GoogleAuthOptions.SectionName).Bind(options);

        options.ClientId = Coalesce(options.ClientId, _configuration["GOOGLE_CLIENT_ID"]);
        options.ClientSecret = Coalesce(options.ClientSecret, _configuration["GOOGLE_CLIENT_SECRET"]);
        options.CallbackUrl = Coalesce(options.CallbackUrl, _configuration["GOOGLE_CALLBACK_URL"]);
        options.FrontendUrl = Coalesce(options.FrontendUrl, _configuration["FRONTEND_URL"]);

        // Trim so a stray newline in an env var cannot corrupt a URL or the audience check.
        options.ClientId = options.ClientId?.Trim();
        options.ClientSecret = options.ClientSecret?.Trim();
        options.CallbackUrl = options.CallbackUrl?.Trim().TrimEnd('/');
        options.FrontendUrl = options.FrontendUrl?.Trim().TrimEnd('/');

        if (options.ExchangeCodeLifetimeSeconds <= 0)
        {
            options.ExchangeCodeLifetimeSeconds = 90;
        }
    }

    /// <summary>Structured value wins; the flat variable fills in only when it is blank.</summary>
    private static string? Coalesce(string? preferred, string? fallback) =>
        string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
}
