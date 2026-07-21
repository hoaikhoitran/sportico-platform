using Microsoft.Extensions.Options;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Api.Middlewares;

/// <summary>
/// First-party, self-hosted visitor tracking (no Google Analytics, no external service — everything
/// is written to our own database). Identifies visitors with a long-lived anonymous cookie, never a
/// raw IP. Records BACKEND API usage only (ApiRequestMetric) — frontend page views are a distinct
/// concept the frontend submits explicitly via POST /api/analytics/pageview (see AnalyticsController
/// and PageView), because this middleware cannot observe client-side route changes.
///
/// Everything in this class is synchronous, in-memory, and non-blocking: no DbContext, no scoped
/// service, no I/O of any kind. The visitor cookie/identity is resolved here, but actual persistence
/// is handed off to IVisitorTrackingQueue (a Channel&lt;T&gt;) and performed later, off the request
/// thread, by VisitorTrackingBackgroundService — so tracking adds ZERO measurable I/O latency to the
/// request and a tracking failure can NEVER surface as an error on a real (business) request.
/// </summary>
public class VisitorTrackingMiddleware
{
    /// <summary>
    /// HttpContext.Items key the resolved VisitorId is published under for this same request, so
    /// downstream code (e.g. AnalyticsController) can read it without re-deriving it.
    /// </summary>
    public const string VisitorIdItemKey = "SporticoApp.VisitorId";

    private const string VisitorCookieName = "spt_vid";

    /// <summary>Not real visits at all (infra/tooling) — never tracked, regardless of configuration.</summary>
    private static readonly string[] NeverTrackPathPrefixes =
    {
        "/swagger", "/favicon.ico", "/_framework", "/.well-known"
    };

    /// <summary>
    /// Real visits (the visitor cookie/session is still resolved), but excluded from the
    /// ApiRequestMetric count specifically:
    ///  - /api/admin/analytics, /api/admin/payments — an admin polling their OWN dashboards must not
    ///    inflate "site traffic" analytics with their own back-office usage.
    ///  - /api/analytics/pageview — the page-view submission itself already produces a PageView row;
    ///    also recording it as a generic API hit would double-count the same navigation event.
    /// </summary>
    private static readonly string[] ExcludedFromApiMetricsPathPrefixes =
    {
        "/api/admin/analytics", "/api/admin/payments", "/api/analytics/pageview"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<VisitorTrackingMiddleware> _logger;
    private readonly IUserAgentParser _userAgentParser;
    private readonly IVisitorTrackingQueue _queue;
    private readonly AnalyticsOptions _options;

    // IUserAgentParser and IVisitorTrackingQueue are both Singletons — safe to constructor-inject
    // into this (effectively singleton) middleware. Nothing scoped is ever touched here.
    public VisitorTrackingMiddleware(
        RequestDelegate next,
        ILogger<VisitorTrackingMiddleware> logger,
        IUserAgentParser userAgentParser,
        IVisitorTrackingQueue queue,
        IOptions<AnalyticsOptions> options)
    {
        _next = next;
        _logger = logger;
        _userAgentParser = userAgentParser;
        _queue = queue;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || !IsTrackablePath(context) || IsBot(context))
        {
            await _next(context);
            return;
        }

        var (visitorId, isNewCookie) = ResolveVisitorId(context);
        if (isNewCookie)
        {
            SetVisitorCookie(context, visitorId);
        }

        // Published for AnalyticsController (frontend page-view ingestion) on this same request.
        context.Items[VisitorIdItemKey] = visitorId;

        await _next(context);

        if (IsExcludedFromApiMetrics(context))
        {
            return;
        }

        // The response has already been produced by _next(context) above — nothing past this point
        // may ever throw out of InvokeAsync, or a tracking-side fault would turn an already-successful
        // business response into a 500 via the outer ExceptionMiddleware. TryEnqueue on the real
        // Channel-backed queue does not throw in practice, but this try/catch is the actual guarantee,
        // not an assumption about a BCL implementation detail.
        try
        {
            var enqueued = _queue.TryEnqueue(new VisitorTrackingWorkItem
            {
                Kind = VisitorTrackingWorkItemKind.ApiRequest,
                Context = BuildVisitContext(context, visitorId),
                Path = context.Request.Path.Value ?? "/",
                Method = context.Request.Method,
                StatusCode = context.Response.StatusCode
            });

            if (!enqueued)
            {
                // Queue full under extreme load — dropped by design (see IVisitorTrackingQueue).
                // Logged so sustained drops are visible in ops; never affects the sent response.
                _logger.LogWarning(
                    "Visitor tracking queue is full; dropped API-request metric for {Method} {Path}.",
                    context.Request.Method, context.Request.Path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Visitor tracking enqueue failed for {Method} {Path} (request already completed; no impact on it).",
                context.Request.Method, context.Request.Path);
        }
    }

    private static VisitContext BuildVisitContext(HttpContext context, Guid visitorId) => new()
    {
        VisitorId = visitorId,
        IpAddress = VisitorSignalResolver.ResolveClientIp(context),
        UserAgent = VisitorSignalResolver.ResolveUserAgent(context),
        Country = VisitorSignalResolver.ResolveCountry(context),
        UserId = context.User?.GetUserIdOrNull()
    };

    private static bool IsTrackablePath(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        return !NeverTrackPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsExcludedFromApiMetrics(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        return ExcludedFromApiMetricsPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsBot(HttpContext context)
    {
        if (_options.TrackBots)
        {
            return false;
        }

        var ua = VisitorSignalResolver.ResolveUserAgent(context);
        return _userAgentParser.Parse(ua).Device == DeviceTypes.Bot;
    }

    private static (Guid VisitorId, bool IsNewCookie) ResolveVisitorId(HttpContext context)
    {
        var raw = context.Request.Cookies[VisitorCookieName];
        if (Guid.TryParse(raw, out var existing))
        {
            return (existing, false);
        }

        return (Guid.NewGuid(), true);
    }

    private static void SetVisitorCookie(HttpContext context, Guid visitorId)
    {
        context.Response.Cookies.Append(VisitorCookieName, visitorId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true
        });
    }
}
