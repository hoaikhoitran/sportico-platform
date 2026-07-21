using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Helpers;

namespace SporticoApp.Application.Services
{
    /// <summary>
    /// Write-side visitor tracking. Only ever invoked by VisitorTrackingBackgroundService, off the
    /// HTTP request thread — never awaited inline by the middleware/controller. A single request's
    /// worth of work here is at most 2 reads + 1 SaveChanges (see class remarks in
    /// VisitorTrackingBackgroundService for the full latency/query-count accounting).
    /// </summary>
    public class VisitorTrackingService : IVisitorTrackingService
    {
        private readonly IVisitorTrackingRepository _repository;
        private readonly IUserAgentParser _userAgentParser;
        private readonly AnalyticsOptions _options;

        public VisitorTrackingService(
            IVisitorTrackingRepository repository,
            IUserAgentParser userAgentParser,
            IOptions<AnalyticsOptions> options)
        {
            _repository = repository;
            _userAgentParser = userAgentParser;
            _options = options.Value;
        }

        public async Task TrackApiRequestAsync(VisitContext context, string path, string method, int statusCode)
        {
            if (!_options.Enabled)
            {
                return;
            }

            var session = await GetOrCreateSessionAsync(context);

            session.ApiRequestCount++;
            Touch(session);

            await _repository.AddApiRequestMetricWithoutSaveAsync(new ApiRequestMetric
            {
                Id = Guid.NewGuid(),
                VisitorSessionId = session.Id,
                UserId = context.UserId,
                Path = Truncate(path, 500) ?? "/",
                Method = method,
                StatusCode = statusCode,
                RequestedAt = session.LastSeenAt
            });

            await _repository.SaveChangesAsync();
        }

        public async Task TrackPageViewAsync(VisitContext context, string path, string? title, string? referrer)
        {
            if (!_options.Enabled)
            {
                return;
            }

            var session = await GetOrCreateSessionAsync(context);

            session.PageViewCount++;
            Touch(session);

            await _repository.AddPageViewWithoutSaveAsync(new PageView
            {
                Id = Guid.NewGuid(),
                VisitorSessionId = session.Id,
                UserId = context.UserId,
                Path = Truncate(path, 500) ?? "/",
                Title = Truncate(title, 200),
                Referrer = Truncate(referrer, 500),
                ViewedAt = session.LastSeenAt
            });

            await _repository.SaveChangesAsync();
        }

        /// <summary>
        /// Shared session-resolution logic for both tracked activities: reuse the visitor's still-open
        /// session (LastSeenAt within SessionIdleMinutes) or start a new one, classifying new-vs-
        /// returning BEFORE the new row exists (else a visitor's very first session would already see
        /// itself and look "returning").
        /// </summary>
        private async Task<VisitorSession> GetOrCreateSessionAsync(VisitContext context)
        {
            var now = DateTime.UtcNow;
            var idleSince = now.AddMinutes(-Math.Max(1, _options.SessionIdleMinutes));

            var session = await _repository.GetOpenSessionForUpdateAsync(context.VisitorId, idleSince);

            if (session != null)
            {
                // Attribute an anonymous-started session to the user once they authenticate mid-session.
                session.UserId ??= context.UserId;
                return session;
            }

            var hasPriorSession = await _repository.HasPriorSessionAsync(context.VisitorId);
            var uaInfo = _userAgentParser.Parse(context.UserAgent);

            session = new VisitorSession
            {
                Id = Guid.NewGuid(),
                VisitorId = context.VisitorId,
                IpHash = IpHasher.Hash(context.IpAddress, _options.IpHashSalt),
                UserAgent = Truncate(context.UserAgent, 500),
                Device = uaInfo.Device,
                Browser = uaInfo.Browser,
                Os = uaInfo.Os,
                Country = context.Country,
                UserId = context.UserId,
                IsNewVisitor = !hasPriorSession,
                PageViewCount = 0,
                ApiRequestCount = 0,
                FirstSeenAt = now,
                LastSeenAt = now,
                DurationSeconds = 0,
                CreatedAt = now
            };

            await _repository.AddSessionWithoutSaveAsync(session);
            return session;
        }

        /// <summary>
        /// LastSeenAt := now; DurationSeconds := LastSeenAt − FirstSeenAt. Because GetOrCreateSessionAsync
        /// only reuses a session while LastSeenAt is within SessionIdleMinutes, no two consecutive
        /// touches on the SAME session can ever be more than SessionIdleMinutes apart — an idle gap
        /// larger than that always starts a brand new session instead. DurationSeconds can therefore
        /// never be inflated by idle time; it is exactly the wall-clock span of actual activity.
        /// </summary>
        private static void Touch(VisitorSession session)
        {
            var now = DateTime.UtcNow;
            session.LastSeenAt = now;
            session.DurationSeconds = (int)(session.LastSeenAt - session.FirstSeenAt).TotalSeconds;
        }

        private static string? Truncate(string? value, int maxLength)
            => string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
    }
}
