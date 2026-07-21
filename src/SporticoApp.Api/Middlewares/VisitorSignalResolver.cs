namespace SporticoApp.Api.Middlewares;

/// <summary>
/// Shared client-signal extraction reused by both VisitorTrackingMiddleware (backend API requests)
/// and AnalyticsController (frontend page-view submissions) — one implementation, not duplicated.
/// </summary>
public static class VisitorSignalResolver
{
    /// <summary>Prefers X-Forwarded-For (set by the Azure/reverse-proxy front end) over the socket IP.</summary>
    public static string? ResolveClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Best-effort geo lookup with no external service/API key: reads a country header set by a
    /// CDN/reverse-proxy in front of the app, if any (e.g. Cloudflare's CF-IPCountry). Null when no
    /// such proxy is in the request path — see docs/api limitations for a real GeoIP option.
    /// </summary>
    public static string? ResolveCountry(HttpContext context)
    {
        var cfCountry = context.Request.Headers["CF-IPCountry"].ToString();
        if (!string.IsNullOrWhiteSpace(cfCountry))
        {
            return cfCountry;
        }

        var xCountry = context.Request.Headers["X-Country-Code"].ToString();
        return string.IsNullOrWhiteSpace(xCountry) ? null : xCountry;
    }

    public static string ResolveUserAgent(HttpContext context)
        => context.Request.Headers.UserAgent.ToString();
}
