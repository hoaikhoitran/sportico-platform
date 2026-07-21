using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;
using SporticoApp.Application.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Analytics;

/// <summary>
/// Covers: session reuse within the idle window, session expiration (a new session starts after
/// the idle timeout), new-vs-returning visitor classification, and that DurationSeconds can never
/// be inflated by an idle gap (see Touch() remarks in VisitorTrackingService).
/// </summary>
public class VisitorTrackingServiceTests
{
    private static readonly Guid VisitorId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static VisitorTrackingService Build(FakeVisitorTrackingRepository repo, int idleMinutes = 30)
        => new(repo, new FakeUserAgentParser(), Microsoft.Extensions.Options.Options.Create(new AnalyticsOptions
        {
            SessionIdleMinutes = idleMinutes,
            IpHashSalt = "test-salt"
        }));

    private static VisitContext Ctx(Guid? userId = null) => new()
    {
        VisitorId = VisitorId,
        IpAddress = "203.0.113.10",
        UserAgent = "TestAgent/1.0",
        Country = "VN",
        UserId = userId
    };

    // 1. First-ever visit: no prior session exists → IsNewVisitor = true.
    [Fact]
    public async Task FirstVisit_NoPriorSession_MarksNewVisitor()
    {
        var repo = new FakeVisitorTrackingRepository();
        var svc = Build(repo);

        await svc.TrackApiRequestAsync(Ctx(), "/api/x", "GET", 200);

        var session = Assert.Single(repo.Sessions);
        Assert.True(session.IsNewVisitor);
        Assert.Equal(1, session.ApiRequestCount);
        Assert.Equal(0, session.PageViewCount);
    }

    // 2. Two touches within the idle window reuse the SAME session (no duplicate session row).
    [Fact]
    public async Task SecondTouch_WithinIdleWindow_ReusesSameSession()
    {
        var repo = new FakeVisitorTrackingRepository();
        var svc = Build(repo, idleMinutes: 30);

        await svc.TrackApiRequestAsync(Ctx(), "/api/x", "GET", 200);
        var firstSessionId = repo.Sessions.Single().Id;

        // Simulate 10 minutes passing (still within the 30-minute idle window).
        repo.Sessions.Single().LastSeenAt = DateTime.UtcNow.AddMinutes(-10);

        await svc.TrackPageViewAsync(Ctx(), "/coaches/1", null, null);

        var session = Assert.Single(repo.Sessions); // still exactly one session
        Assert.Equal(firstSessionId, session.Id);
        Assert.Equal(1, session.ApiRequestCount);
        Assert.Equal(1, session.PageViewCount); // separate counters, both incremented correctly
    }

    // 3. A touch AFTER the idle timeout starts a brand new session (session expiration).
    [Fact]
    public async Task TouchAfterIdleTimeout_StartsNewSession()
    {
        var repo = new FakeVisitorTrackingRepository();
        var svc = Build(repo, idleMinutes: 30);

        await svc.TrackApiRequestAsync(Ctx(), "/api/x", "GET", 200);

        // Simulate 40 minutes of inactivity — beyond the 30-minute idle window.
        repo.Sessions.Single().LastSeenAt = DateTime.UtcNow.AddMinutes(-40);

        await svc.TrackApiRequestAsync(Ctx(), "/api/y", "GET", 200);

        Assert.Equal(2, repo.Sessions.Count); // a genuinely new session was created
    }

    // 4. A visitor's SECOND-EVER session (in a later, separate visit) is classified as returning.
    [Fact]
    public async Task SecondSeparateVisit_IsClassifiedAsReturning()
    {
        var repo = new FakeVisitorTrackingRepository();
        var svc = Build(repo, idleMinutes: 30);

        await svc.TrackApiRequestAsync(Ctx(), "/api/x", "GET", 200);
        Assert.True(repo.Sessions.Single().IsNewVisitor);

        // Force expiration so the next touch starts a new session for the SAME VisitorId.
        repo.Sessions.Single().LastSeenAt = DateTime.UtcNow.AddMinutes(-40);

        await svc.TrackApiRequestAsync(Ctx(), "/api/y", "GET", 200);

        Assert.Equal(2, repo.Sessions.Count);
        var newestSession = repo.Sessions.OrderByDescending(s => s.CreatedAt).First();
        Assert.False(newestSession.IsNewVisitor); // returning, because HasPriorSessionAsync now sees the first one
    }

    // 5. DurationSeconds formula: within one open session, duration = LastSeenAt - FirstSeenAt,
    // never inflated by idle time (an idle gap that large would have started a new session instead).
    [Fact]
    public async Task DurationSeconds_ReflectsElapsedActivity_NotIdleGaps()
    {
        var repo = new FakeVisitorTrackingRepository();
        var svc = Build(repo, idleMinutes: 30);

        await svc.TrackApiRequestAsync(Ctx(), "/api/x", "GET", 200);
        var session = repo.Sessions.Single();
        var firstSeen = session.FirstSeenAt;

        session.LastSeenAt = DateTime.UtcNow.AddMinutes(-5); // 5 minutes ago, still within idle window

        await svc.TrackPageViewAsync(Ctx(), "/p", null, null);

        var updated = repo.Sessions.Single();
        Assert.Equal(firstSeen, updated.FirstSeenAt); // FirstSeenAt never changes
        Assert.True(updated.DurationSeconds >= 0 && updated.DurationSeconds < 60,
            $"Expected a small duration close to 0s, got {updated.DurationSeconds}s");
    }

    // 6. Mid-session login attributes an anonymous-started session to the now-known user.
    [Fact]
    public async Task MidSessionLogin_AttributesExistingSessionToUser()
    {
        var repo = new FakeVisitorTrackingRepository();
        var svc = Build(repo);
        var userId = Guid.NewGuid();

        await svc.TrackApiRequestAsync(Ctx(userId: null), "/api/x", "GET", 200);
        Assert.Null(repo.Sessions.Single().UserId);

        await svc.TrackApiRequestAsync(Ctx(userId: userId), "/api/y", "GET", 200);

        Assert.Equal(userId, repo.Sessions.Single().UserId);
    }

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeVisitorTrackingRepository : IVisitorTrackingRepository
    {
        public readonly List<VisitorSession> Sessions = new();
        public readonly List<ApiRequestMetric> ApiRequestMetrics = new();
        public readonly List<PageView> PageViews = new();

        public Task<bool> HasPriorSessionAsync(Guid visitorId)
            => Task.FromResult(Sessions.Any(s => s.VisitorId == visitorId));

        public Task<VisitorSession?> GetOpenSessionForUpdateAsync(Guid visitorId, DateTime idleSinceUtc)
            => Task.FromResult(Sessions
                .Where(s => s.VisitorId == visitorId && s.LastSeenAt >= idleSinceUtc)
                .OrderByDescending(s => s.LastSeenAt)
                .FirstOrDefault());

        public Task AddSessionWithoutSaveAsync(VisitorSession session)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task AddApiRequestMetricWithoutSaveAsync(ApiRequestMetric metric)
        {
            ApiRequestMetrics.Add(metric);
            return Task.CompletedTask;
        }

        public Task AddPageViewWithoutSaveAsync(PageView pageView)
        {
            PageViews.Add(pageView);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeUserAgentParser : IUserAgentParser
    {
        public UserAgentInfo Parse(string? userAgent) => new()
        {
            Device = DeviceTypes.Desktop,
            Browser = "Chrome",
            Os = "Windows"
        };
    }
}
