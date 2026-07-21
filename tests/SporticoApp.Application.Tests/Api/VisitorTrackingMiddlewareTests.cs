using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SporticoApp.Api.Middlewares;
using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Api;

/// <summary>
/// Covers: excluded paths (swagger/favicon/OPTIONS/admin's-own-analytics-and-payments-endpoints/the
/// page-view ingestion endpoint), bot exclusion, and — critically — that a tracking-side failure
/// (queue throwing) can NEVER surface as an error on the business request the middleware wraps.
/// </summary>
public class VisitorTrackingMiddlewareTests
{
    private static VisitorTrackingMiddleware Build(
        IVisitorTrackingQueue queue,
        RequestDelegate? next = null,
        bool trackBots = false,
        bool enabled = true)
    {
        next ??= _ => Task.CompletedTask;
        var options = Microsoft.Extensions.Options.Options.Create(new AnalyticsOptions { Enabled = enabled, TrackBots = trackBots });
        return new VisitorTrackingMiddleware(
            next, NullLogger<VisitorTrackingMiddleware>.Instance, new FakeUserAgentParser(), queue, options);
    }

    private static DefaultHttpContext Context(string path, string method = "GET", string userAgent = "Mozilla/5.0 TestBrowser")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Method = method;
        ctx.Request.Headers.UserAgent = userAgent;
        return ctx;
    }

    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/favicon.ico")]
    [InlineData("/_framework/blazor.js")]
    [InlineData("/.well-known/security.txt")]
    public async Task InfraPaths_AreNeverTracked(string path)
    {
        var queue = new FakeQueue();
        var middleware = Build(queue);
        var ctx = Context(path);

        await middleware.InvokeAsync(ctx);

        Assert.Empty(queue.Enqueued);
        Assert.False(ctx.Items.ContainsKey(VisitorTrackingMiddleware.VisitorIdItemKey));
    }

    [Fact]
    public async Task OptionsRequest_IsNeverTracked()
    {
        var queue = new FakeQueue();
        var middleware = Build(queue);
        var ctx = Context("/api/bookings/purchase/payos", method: "OPTIONS");

        await middleware.InvokeAsync(ctx);

        Assert.Empty(queue.Enqueued);
    }

    [Theory]
    [InlineData("/api/admin/analytics/dashboard")]
    [InlineData("/api/admin/payments/dashboard")]
    [InlineData("/api/analytics/pageview")]
    public async Task ExcludedFromApiMetrics_VisitorStillResolved_ButNoMetricEnqueued(string path)
    {
        var queue = new FakeQueue();
        var middleware = Build(queue);
        var ctx = Context(path);

        await middleware.InvokeAsync(ctx);

        // Visitor identity IS resolved (so e.g. the pageview controller can still read it)...
        Assert.True(ctx.Items.ContainsKey(VisitorTrackingMiddleware.VisitorIdItemKey));
        // ...but this specific hit is not recorded as an ApiRequestMetric (self-polling/double-count guard).
        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task NormalApiPath_IsTrackedAsApiRequestMetric()
    {
        var queue = new FakeQueue();
        var middleware = Build(queue);
        var ctx = Context("/api/public/training-packages");

        await middleware.InvokeAsync(ctx);

        var item = Assert.Single(queue.Enqueued);
        Assert.Equal(VisitorTrackingWorkItemKind.ApiRequest, item.Kind);
        Assert.Equal("/api/public/training-packages", item.Path);
    }

    [Fact]
    public async Task VisitorCookie_MintedOnce_ReusedOnSubsequentRequest()
    {
        var queue = new FakeQueue();
        var middleware = Build(queue);
        var ctx1 = Context("/api/public/training-packages");

        await middleware.InvokeAsync(ctx1);

        var setCookieHeader = ctx1.Response.Headers.SetCookie.ToString();
        Assert.Contains("spt_vid=", setCookieHeader);
        Assert.Contains("httponly", setCookieHeader, StringComparison.OrdinalIgnoreCase);

        // Simulate the browser sending the cookie back on the next request.
        var visitorId = (Guid)ctx1.Items[VisitorTrackingMiddleware.VisitorIdItemKey]!;
        var ctx2 = Context("/api/public/training-packages");
        ctx2.Request.Headers.Cookie = $"spt_vid={visitorId}";

        await middleware.InvokeAsync(ctx2);

        Assert.False(ctx2.Response.Headers.ContainsKey("Set-Cookie")); // no new cookie minted
        Assert.Equal(visitorId, ctx2.Items[VisitorTrackingMiddleware.VisitorIdItemKey]);
    }

    [Fact]
    public async Task BotUserAgent_TrackBotsDisabled_NeverTracked()
    {
        var queue = new FakeQueue();
        var middleware = Build(queue, trackBots: false);
        var ctx = Context("/api/public/training-packages", userAgent: "Googlebot/2.1 (+http://www.google.com/bot.html)");

        await middleware.InvokeAsync(ctx);

        Assert.Empty(queue.Enqueued);
        Assert.False(ctx.Items.ContainsKey(VisitorTrackingMiddleware.VisitorIdItemKey));
    }

    [Fact]
    public async Task BotUserAgent_TrackBotsEnabled_TrackedNormally()
    {
        var queue = new FakeQueue();
        var middleware = Build(queue, trackBots: true);
        var ctx = Context("/api/public/training-packages", userAgent: "Googlebot/2.1 (+http://www.google.com/bot.html)");

        await middleware.InvokeAsync(ctx);

        Assert.Single(queue.Enqueued);
    }

    [Fact]
    public async Task AnalyticsDisabled_NothingIsTracked()
    {
        var queue = new FakeQueue();
        var middleware = Build(queue, enabled: false);
        var ctx = Context("/api/public/training-packages");

        await middleware.InvokeAsync(ctx);

        Assert.Empty(queue.Enqueued);
        Assert.False(ctx.Items.ContainsKey(VisitorTrackingMiddleware.VisitorIdItemKey));
    }

    // Critical: the business request must complete successfully even when the tracking queue
    // itself throws — a tracking-side failure must never surface as an error on a real request.
    [Fact]
    public async Task QueueThrows_BusinessRequestStillCompletesSuccessfully()
    {
        var queue = new ThrowingQueue();
        var nextCalled = false;
        var middleware = Build(queue, next: ctx => { nextCalled = true; ctx.Response.StatusCode = 200; return Task.CompletedTask; });
        var ctx = Context("/api/public/training-packages");

        var exception = await Record.ExceptionAsync(() => middleware.InvokeAsync(ctx));

        Assert.Null(exception);
        Assert.True(nextCalled);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeQueue : IVisitorTrackingQueue
    {
        public readonly List<VisitorTrackingWorkItem> Enqueued = new();

        public bool TryEnqueue(VisitorTrackingWorkItem item)
        {
            Enqueued.Add(item);
            return true;
        }

        public IAsyncEnumerable<VisitorTrackingWorkItem> ReadAllAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private sealed class ThrowingQueue : IVisitorTrackingQueue
    {
        public bool TryEnqueue(VisitorTrackingWorkItem item) => throw new InvalidOperationException("simulated queue failure");

        public IAsyncEnumerable<VisitorTrackingWorkItem> ReadAllAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private sealed class FakeUserAgentParser : IUserAgentParser
    {
        public UserAgentInfo Parse(string? userAgent)
        {
            var ua = userAgent ?? string.Empty;
            return new UserAgentInfo
            {
                Device = ua.Contains("bot", StringComparison.OrdinalIgnoreCase) ? DeviceTypes.Bot : DeviceTypes.Desktop,
                Browser = "Chrome",
                Os = "Windows"
            };
        }
    }
}
