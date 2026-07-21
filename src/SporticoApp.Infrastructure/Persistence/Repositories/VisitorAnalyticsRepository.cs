using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.VisitorAnalytics;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Options;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Helpers;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Read-only aggregate queries for the admin visitor-analytics dashboard — same server-side
    /// aggregation style as <see cref="DashboardRepository"/> / <see cref="AdminPaymentRepository"/>.
    /// "Today / this week / this month" and the chart's bucket boundaries are computed in
    /// Asia/Ho_Chi_Minh business time (see <see cref="VietnamTimeZone"/>) so they reconcile with
    /// each other, exactly as documented on <see cref="AdminPaymentRepository"/>.
    /// </summary>
    public class VisitorAnalyticsRepository : IVisitorAnalyticsRepository
    {
        private readonly AppDbContext _context;
        private readonly AnalyticsOptions _options;

        public VisitorAnalyticsRepository(AppDbContext context, IOptions<AnalyticsOptions> options)
        {
            _context = context;
            _options = options.Value;
        }

        public async Task<VisitorStatsResponse> GetVisitorStatsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var sessions = ApplySessionRange(_context.VisitorSessions.AsNoTracking(), fromDate, toDate);

            var result = new VisitorStatsResponse
            {
                TotalVisitors = await sessions.Select(s => s.VisitorId).Distinct().CountAsync(),
                ReturningVisitors = await sessions.Where(s => !s.IsNewVisitor)
                    .Select(s => s.VisitorId).Distinct().CountAsync(),
                NewVisitors = await sessions.Where(s => s.IsNewVisitor)
                    .Select(s => s.VisitorId).Distinct().CountAsync()
            };

            var now = DateTime.UtcNow;
            result.TodayVisitors = await CountDistinctVisitorsSinceAsync(VietnamTimeZone.StartOfDayUtc(now));
            result.WeeklyVisitors = await CountDistinctVisitorsSinceAsync(VietnamTimeZone.StartOfWeekUtc(now));
            result.MonthlyVisitors = await CountDistinctVisitorsSinceAsync(VietnamTimeZone.StartOfMonthUtc(now));

            var activeSince = now.AddMinutes(-Math.Max(1, _options.ActiveWindowMinutes));
            result.ActiveVisitors = await _context.VisitorSessions.AsNoTracking()
                .Where(s => s.LastSeenAt >= activeSince)
                .Select(s => s.VisitorId)
                .Distinct()
                .CountAsync();

            return result;
        }

        public async Task<PageViewStatsResponse> GetPageViewStatsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var pageViews = ApplyPageViewRange(_context.PageViews.AsNoTracking(), fromDate, toDate);
            var sessions = ApplySessionRange(_context.VisitorSessions.AsNoTracking(), fromDate, toDate);

            var result = new PageViewStatsResponse
            {
                TotalPageViews = await pageViews.CountAsync(),
                AveragePageViewsPerSession = await sessions.AverageAsync(s => (decimal?)s.PageViewCount) ?? 0m
            };

            var todayStartUtc = VietnamTimeZone.StartOfDayUtc(DateTime.UtcNow);
            result.TodayPageViews = await _context.PageViews.AsNoTracking()
                .CountAsync(p => p.ViewedAt >= todayStartUtc);

            return result;
        }

        public async Task<List<TopPageItem>> GetTopPagesAsync(DateTime? fromDate, DateTime? toDate, int limit)
        {
            var pageViews = ApplyPageViewRange(_context.PageViews.AsNoTracking(), fromDate, toDate);

            var grouped = await pageViews
                .GroupBy(p => p.Path)
                .Select(g => new
                {
                    Path = g.Key,
                    ViewCount = g.Count(),
                    // Distinct SESSIONS (a plain column already on this table) rather than distinct
                    // historical VisitorIds — a single flat GROUP BY with no join/correlated subquery.
                    UniqueVisitors = g.Select(x => x.VisitorSessionId).Distinct().Count()
                })
                .OrderByDescending(x => x.ViewCount)
                .Take(limit)
                .ToListAsync();

            return grouped
                .Select(g => new TopPageItem { Path = g.Path, ViewCount = g.ViewCount, UniqueVisitors = g.UniqueVisitors })
                .ToList();
        }

        public async Task<List<DeviceBreakdownItem>> GetDeviceBreakdownAsync(DateTime? fromDate, DateTime? toDate)
        {
            var sessions = ApplySessionRange(_context.VisitorSessions.AsNoTracking(), fromDate, toDate);

            var totalCount = await sessions.CountAsync();
            if (totalCount == 0)
            {
                return new List<DeviceBreakdownItem>();
            }

            var grouped = await sessions
                .GroupBy(s => s.Device ?? DeviceTypes.Unknown)
                .Select(g => new { Device = g.Key, Count = g.Count() })
                .ToListAsync();

            return grouped
                .Select(g => new DeviceBreakdownItem { Device = g.Device, Count = g.Count, Percentage = Percentage(g.Count, totalCount) })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        public async Task<List<BrowserBreakdownItem>> GetBrowserBreakdownAsync(DateTime? fromDate, DateTime? toDate)
        {
            var sessions = ApplySessionRange(_context.VisitorSessions.AsNoTracking(), fromDate, toDate);

            var totalCount = await sessions.CountAsync();
            if (totalCount == 0)
            {
                return new List<BrowserBreakdownItem>();
            }

            var grouped = await sessions
                .GroupBy(s => s.Browser ?? "Unknown")
                .Select(g => new { Browser = g.Key, Count = g.Count() })
                .ToListAsync();

            return grouped
                .Select(g => new BrowserBreakdownItem { Browser = g.Browser, Count = g.Count, Percentage = Percentage(g.Count, totalCount) })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        public async Task<List<CountryBreakdownItem>> GetCountryBreakdownAsync(DateTime? fromDate, DateTime? toDate)
        {
            var sessions = ApplySessionRange(_context.VisitorSessions.AsNoTracking(), fromDate, toDate);

            var totalCount = await sessions.CountAsync();
            if (totalCount == 0)
            {
                return new List<CountryBreakdownItem>();
            }

            var grouped = await sessions
                .GroupBy(s => s.Country ?? "Unknown")
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .ToListAsync();

            return grouped
                .Select(g => new CountryBreakdownItem { Country = g.Country, Count = g.Count, Percentage = Percentage(g.Count, totalCount) })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        public async Task<List<VisitorsChartPoint>> GetVisitorsChartAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string granularity)
        {
            // Whitelisted to exactly these 5 literals before ever reaching SQL; also passed as a
            // bound parameter below (defense in depth — never string-concatenated into the query).
            var field = NormalizeGranularity(granularity);
            var fromBound = fromDate ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            var toBound = toDate ?? new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Two true server-side aggregations (Postgres date_trunc), one per source table — visitor
            // counts are keyed on session FirstSeenAt, page-view counts on PageView.ViewedAt, so they
            // are queried and bucketed independently, then merged by period below. "hour" granularity
            // has no meaningful VN-vs-UTC distinction beyond the fixed +7 offset already applied by
            // AT TIME ZONE, so the same idiom is used for all 5 granularities.
            var visitorRows = await _context.Database.SqlQuery<VisitorBucketRow>($@"
                SELECT
                    (date_trunc({field}, s.first_seen_at AT TIME ZONE 'Asia/Ho_Chi_Minh') AT TIME ZONE 'Asia/Ho_Chi_Minh') AS ""PeriodStart"",
                    COUNT(DISTINCT s.visitor_id)::int AS ""Count""
                FROM visitor_sessions s
                WHERE s.first_seen_at >= {fromBound} AND s.first_seen_at <= {toBound}
                GROUP BY 1
            ").ToListAsync();

            var pageViewRows = await _context.Database.SqlQuery<VisitorBucketRow>($@"
                SELECT
                    (date_trunc({field}, p.viewed_at AT TIME ZONE 'Asia/Ho_Chi_Minh') AT TIME ZONE 'Asia/Ho_Chi_Minh') AS ""PeriodStart"",
                    COUNT(*)::int AS ""Count""
                FROM page_views p
                WHERE p.viewed_at >= {fromBound} AND p.viewed_at <= {toBound}
                GROUP BY 1
            ").ToListAsync();

            var visitorByPeriod = visitorRows.ToDictionary(r => r.PeriodStart, r => r.Count);
            var pageViewByPeriod = pageViewRows.ToDictionary(r => r.PeriodStart, r => r.Count);

            var periods = visitorByPeriod.Keys.Union(pageViewByPeriod.Keys).OrderBy(x => x).ToList();

            return periods
                .Select(period => new VisitorsChartPoint
                {
                    PeriodStart = period,
                    PeriodLabel = FormatPeriodLabel(period, field),
                    VisitorCount = visitorByPeriod.TryGetValue(period, out var vc) ? vc : 0,
                    PageViewCount = pageViewByPeriod.TryGetValue(period, out var pc) ? pc : 0
                })
                .ToList();
        }

        // ── shared helpers ──────────────────────────────────────────────────────

        private async Task<int> CountDistinctVisitorsSinceAsync(DateTime sinceUtc)
            => await _context.VisitorSessions.AsNoTracking()
                .Where(s => s.FirstSeenAt >= sinceUtc)
                .Select(s => s.VisitorId)
                .Distinct()
                .CountAsync();

        private static IQueryable<VisitorSession> ApplySessionRange(IQueryable<VisitorSession> query, DateTime? from, DateTime? to)
        {
            if (from.HasValue) query = query.Where(s => s.FirstSeenAt >= from.Value);
            if (to.HasValue) query = query.Where(s => s.FirstSeenAt <= to.Value);
            return query;
        }

        private static IQueryable<PageView> ApplyPageViewRange(IQueryable<PageView> query, DateTime? from, DateTime? to)
        {
            if (from.HasValue) query = query.Where(p => p.ViewedAt >= from.Value);
            if (to.HasValue) query = query.Where(p => p.ViewedAt <= to.Value);
            return query;
        }

        /// <summary>Whitelist — the ONLY values ever bound into the date_trunc(...) SQL parameter.</summary>
        private static string NormalizeGranularity(string? granularity) => granularity?.Trim().ToLowerInvariant() switch
        {
            "hour" => "hour",
            "week" => "week",
            "month" => "month",
            "year" => "year",
            _ => "day"
        };

        private static string FormatPeriodLabel(DateTime periodStart, string granularity) => granularity switch
        {
            "hour" => periodStart.ToString("yyyy-MM-dd HH:00"),
            "week" => $"{periodStart:yyyy}-W{System.Globalization.ISOWeek.GetWeekOfYear(periodStart):D2}",
            "month" => periodStart.ToString("yyyy-MM"),
            "year" => periodStart.ToString("yyyy"),
            _ => periodStart.ToString("yyyy-MM-dd")
        };

        private static decimal Percentage(int part, int total)
            => total == 0 ? 0m : Math.Round((decimal)part / total * 100m, 2);

        /// <summary>Ad-hoc projection for the raw date_trunc bucket queries — not an EF entity.</summary>
        private sealed class VisitorBucketRow
        {
            public DateTime PeriodStart { get; set; }
            public int Count { get; set; }
        }
    }
}
