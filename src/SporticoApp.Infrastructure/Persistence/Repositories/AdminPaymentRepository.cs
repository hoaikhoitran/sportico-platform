using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.AdminPayments;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Helpers;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Read-only aggregate queries for the admin payment dashboard. All counts/sums run as
    /// server-side EF/SQL aggregations (no in-memory row materialisation) — same style as
    /// <see cref="DashboardRepository"/>, whose "paid booking" revenue rule this repository
    /// reuses verbatim so the two dashboards can never disagree.
    ///
    /// "Today / this week / this month / this year" and the chart's day-bucket boundaries are all
    /// computed in Asia/Ho_Chi_Minh business time (see <see cref="VietnamTimeZone"/>), not naive UTC
    /// midnight — a booking paid at 00:30 VN time is already "today" in VN even though it is still
    /// the previous UTC calendar day. The week starts on Monday, matching Postgres'
    /// <c>date_trunc('week', ...)</c> default, which the chart query also uses.
    /// </summary>
    public class AdminPaymentRepository : IAdminPaymentRepository
    {
        private readonly AppDbContext _context;

        public AdminPaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentStatisticsResponse> GetStatisticsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var result = new PaymentStatisticsResponse();

            var paidBookings = ApplyBookingRange(PaidBookingsQuery(), fromDate, toDate);

            result.TotalRevenue = await paidBookings.SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;
            result.PlatformRevenue = await paidBookings.SumAsync(b => (decimal?)b.PlatformFeeAmount) ?? 0m;
            result.CoachRevenue = await paidBookings.SumAsync(b => (decimal?)b.CoachReceiveAmount) ?? 0m;
            result.GrossPackageValue = await paidBookings.SumAsync(b => (decimal?)b.OriginalAmount) ?? 0m;
            result.TotalDiscount = await paidBookings.SumAsync(b => (decimal?)b.DiscountAmount) ?? 0m;
            result.NetCollected = result.TotalRevenue;
            result.PlatformGrossFee = result.PlatformRevenue;
            result.PlatformNetRevenue = result.TotalRevenue - result.CoachRevenue;

            var payments = ApplyPaymentRange(_context.Payments.AsNoTracking(), fromDate, toDate);

            result.TotalTransactions = await payments.CountAsync();
            result.PendingTransactions = await payments.CountAsync(p => p.Status == PaymentStatuses.Pending);
            result.FailedTransactions = await payments.CountAsync(p =>
                p.Status == PaymentStatuses.Failed || p.Status == PaymentStatuses.Cancelled);

            var paidPayments = payments.Where(p => p.Status == PaymentStatuses.Paid);
            var paidCount = await paidPayments.CountAsync();

            // A "paid" payment whose linked booking was later refunded is carved out of
            // Successful into Refunded. No flow sets Booking.Status=refunded yet (reserved for a
            // future refund feature), so this is 0 today but wired correctly.
            result.RefundedTransactions = await CountRefundedAsync(paidPayments);
            result.SuccessfulTransactions = paidCount - result.RefundedTransactions;

            result.AverageTransactionValue = await paidPayments.AverageAsync(p => (decimal?)p.Amount) ?? 0m;

            // "As of now" windows are always anchored to the VN business calendar, independent of
            // the FromDate/ToDate range filter — see class remarks.
            var now = DateTime.UtcNow;
            result.RevenueToday = await SumRevenueSincePaidAtAsync(VietnamTimeZone.StartOfDayUtc(now));
            result.RevenueThisWeek = await SumRevenueSincePaidAtAsync(VietnamTimeZone.StartOfWeekUtc(now));
            result.RevenueThisMonth = await SumRevenueSincePaidAtAsync(VietnamTimeZone.StartOfMonthUtc(now));
            result.RevenueThisYear = await SumRevenueSincePaidAtAsync(VietnamTimeZone.StartOfYearUtc(now));

            return result;
        }

        public async Task<List<RevenueChartPoint>> GetRevenueChartAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string granularity)
        {
            // Whitelisted to exactly these 5 literals before ever reaching SQL; also passed as a bound
            // parameter below (defense in depth — never string-concatenated into the query text).
            var field = NormalizeGranularity(granularity);
            var fromBound = fromDate ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            var toBound = toDate ?? new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // True server-side aggregation via Postgres' own date_trunc — no raw rows are pulled into
            // the app. Bucket boundaries are computed in VN local time (AT TIME ZONE) so a "day"
            // bucket lines up with the VN calendar day used by RevenueToday/ThisWeek/etc above, and
            // date_trunc('week', ...) is Postgres' own Monday-start rule (see class remarks).
            // FormattableString interpolation parameterizes EVERY {…} value below (including the
            // granularity literal) — nothing here is string-concatenated user input.
            var rows = await _context.Database.SqlQuery<RevenueChartRow>($@"
                SELECT
                    (date_trunc({field}, b.paid_at AT TIME ZONE 'Asia/Ho_Chi_Minh') AT TIME ZONE 'Asia/Ho_Chi_Minh') AS ""PeriodStart"",
                    COALESCE(SUM(b.total_amount), 0) AS ""GrossRevenue"",
                    COALESCE(SUM(b.platform_fee_amount), 0) AS ""PlatformRevenue"",
                    COALESCE(SUM(b.coach_receive_amount), 0) AS ""CoachRevenue"",
                    COUNT(*)::int AS ""TransactionCount""
                FROM bookings b
                WHERE b.paid_at IS NOT NULL
                  AND b.status IN ('active', 'completed')
                  AND b.created_at >= {fromBound}
                  AND b.created_at <= {toBound}
                GROUP BY 1
                ORDER BY 1
            ").ToListAsync();

            return rows
                .Select(r => new RevenueChartPoint
                {
                    PeriodStart = r.PeriodStart,
                    PeriodLabel = FormatPeriodLabel(r.PeriodStart, field),
                    GrossRevenue = r.GrossRevenue,
                    PlatformRevenue = r.PlatformRevenue,
                    CoachRevenue = r.CoachRevenue,
                    TransactionCount = r.TransactionCount
                })
                .ToList();
        }

        public async Task<List<PaymentMethodBreakdownItem>> GetPaymentMethodBreakdownAsync(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var payments = ApplyPaymentRange(_context.Payments.AsNoTracking(), fromDate, toDate);

            var totalCount = await payments.CountAsync();
            if (totalCount == 0)
            {
                return new List<PaymentMethodBreakdownItem>();
            }

            var grouped = await payments
                .GroupBy(p => p.Method)
                .Select(g => new { Method = g.Key, Count = g.Count(), TotalAmount = g.Sum(x => x.Amount) })
                .ToListAsync();

            return grouped
                .Select(g => new PaymentMethodBreakdownItem
                {
                    Method = g.Method,
                    TransactionCount = g.Count,
                    TotalAmount = g.TotalAmount,
                    Percentage = Percentage(g.Count, totalCount)
                })
                .OrderByDescending(x => x.TransactionCount)
                .ToList();
        }

        public async Task<List<TransactionStatusBreakdownItem>> GetTransactionStatusBreakdownAsync(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var payments = ApplyPaymentRange(_context.Payments.AsNoTracking(), fromDate, toDate);

            var totalCount = await payments.CountAsync();
            if (totalCount == 0)
            {
                return new List<TransactionStatusBreakdownItem>();
            }

            var pending = await payments.CountAsync(p => p.Status == PaymentStatuses.Pending);
            var failed = await payments.CountAsync(p => p.Status == PaymentStatuses.Failed);
            var cancelled = await payments.CountAsync(p => p.Status == PaymentStatuses.Cancelled);

            var paidPayments = payments.Where(p => p.Status == PaymentStatuses.Paid);
            var refunded = await CountRefundedAsync(paidPayments);
            var successfulPaid = await paidPayments.CountAsync() - refunded;

            var items = new List<TransactionStatusBreakdownItem>
            {
                new() { Status = PaymentStatuses.Paid, Count = successfulPaid, Percentage = Percentage(successfulPaid, totalCount) },
                new() { Status = PaymentStatuses.Pending, Count = pending, Percentage = Percentage(pending, totalCount) },
                new() { Status = PaymentStatuses.Failed, Count = failed, Percentage = Percentage(failed, totalCount) },
                new() { Status = PaymentStatuses.Cancelled, Count = cancelled, Percentage = Percentage(cancelled, totalCount) },
                new() { Status = BookingStatuses.Refunded, Count = refunded, Percentage = Percentage(refunded, totalCount) }
            };

            return items.Where(x => x.Count > 0).OrderByDescending(x => x.Count).ToList();
        }

        public async Task<List<TopCoachRevenueItem>> GetTopCoachesAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int limit)
        {
            var query = ApplyBookingRange(PaidBookingsQuery(), fromDate, toDate);

            var grouped = await query
                .GroupBy(b => b.CoachId)
                .Select(g => new
                {
                    CoachId = g.Key,
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    CoachEarnings = g.Sum(x => x.CoachReceiveAmount),
                    PlatformFeeGenerated = g.Sum(x => x.PlatformFeeAmount),
                    BookingCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(limit)
                .ToListAsync();

            if (grouped.Count == 0)
            {
                return new List<TopCoachRevenueItem>();
            }

            // Second, batched lookup (IN-list) — not N+1: one query for all coach names/avatars.
            var coachIds = grouped.Select(x => x.CoachId).ToList();
            var coachInfo = await _context.CoachProfiles.AsNoTracking()
                .Where(c => coachIds.Contains(c.UserId))
                .Select(c => new { c.UserId, c.User.FullName, c.User.AvatarUrl })
                .ToDictionaryAsync(x => x.UserId);

            return grouped
                .Select(g => new TopCoachRevenueItem
                {
                    CoachId = g.CoachId,
                    FullName = coachInfo.TryGetValue(g.CoachId, out var info) ? info.FullName : "Unknown",
                    AvatarUrl = coachInfo.TryGetValue(g.CoachId, out var info2) ? info2.AvatarUrl : null,
                    TotalRevenue = g.TotalRevenue,
                    CoachEarnings = g.CoachEarnings,
                    PlatformFeeGenerated = g.PlatformFeeGenerated,
                    BookingCount = g.BookingCount
                })
                .ToList();
        }

        public async Task<List<TopSportRevenueItem>> GetTopSportsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int limit)
        {
            var query = ApplyBookingRange(PaidBookingsQuery(), fromDate, toDate);

            var grouped = await query
                .GroupBy(b => b.TrainingPackage.SportId)
                .Select(g => new
                {
                    SportId = g.Key,
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    BookingCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(limit)
                .ToListAsync();

            if (grouped.Count == 0)
            {
                return new List<TopSportRevenueItem>();
            }

            var sportIds = grouped.Select(x => x.SportId).ToList();
            var sportNames = await _context.Sports.AsNoTracking()
                .Where(s => sportIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

            return grouped
                .Select(g => new TopSportRevenueItem
                {
                    SportId = g.SportId,
                    SportName = sportNames.TryGetValue(g.SportId, out var name) ? name : "Unknown",
                    TotalRevenue = g.TotalRevenue,
                    BookingCount = g.BookingCount
                })
                .ToList();
        }

        public async Task<(List<AdminTransactionResponse> Items, int TotalCount)> GetTransactionsPagedAsync(
            AdminPaymentFilterRequest filter)
        {
            IQueryable<Payment> query = _context.Payments.AsNoTracking();

            query = ApplyPaymentRange(query, filter.FromDate, filter.ToDate);

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalized = filter.Status.Trim().ToLowerInvariant();
                query = query.Where(p => p.Status.ToLower() == normalized);
            }

            if (!string.IsNullOrWhiteSpace(filter.Method))
            {
                var normalized = filter.Method.Trim().ToLowerInvariant();
                query = query.Where(p => p.Method.ToLower() == normalized);
            }

            var totalCount = await query.CountAsync();

            var sort = filter.SortBy?.Trim().ToLowerInvariant();
            query = sort switch
            {
                "oldest" => query.OrderBy(p => p.CreatedAt),
                "amount_desc" => query.OrderByDescending(p => p.Amount),
                "amount_asc" => query.OrderBy(p => p.Amount),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // Single projected query joining User — no N+1 (User.FullName/Email pulled via SQL join).
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new AdminTransactionResponse
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    UserName = p.User.FullName,
                    UserEmail = p.User.Email,
                    Amount = p.Amount,
                    Method = p.Method,
                    Status = p.Status,
                    ReferenceType = p.ReferenceType,
                    ReferenceId = p.ReferenceId,
                    TransactionCode = p.TransactionCode,
                    OrderCode = p.OrderCode,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt
                })
                .ToListAsync();

            return (items, totalCount);
        }

        // ── shared helpers ──────────────────────────────────────────────────────

        private IQueryable<Booking> PaidBookingsQuery()
            => _context.Bookings.AsNoTracking().Where(b =>
                b.PaidAt != null &&
                (b.Status == BookingStatuses.Active || b.Status == BookingStatuses.Completed));

        private async Task<decimal> SumRevenueSincePaidAtAsync(DateTime sinceUtc)
            => await PaidBookingsQuery()
                .Where(b => b.PaidAt!.Value >= sinceUtc)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;

        private Task<int> CountRefundedAsync(IQueryable<Payment> paidPayments)
        {
            var refundedBookingIds = _context.Bookings.AsNoTracking()
                .Where(b => b.Status == BookingStatuses.Refunded)
                .Select(b => b.Id);

            return paidPayments.CountAsync(p =>
                p.ReferenceType == PaymentReferenceTypes.Booking &&
                p.ReferenceId != null &&
                refundedBookingIds.Contains(p.ReferenceId.Value));
        }

        private static IQueryable<Booking> ApplyBookingRange(IQueryable<Booking> query, DateTime? from, DateTime? to)
        {
            if (from.HasValue) query = query.Where(b => b.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(b => b.CreatedAt <= to.Value);
            return query;
        }

        private static IQueryable<Payment> ApplyPaymentRange(IQueryable<Payment> query, DateTime? from, DateTime? to)
        {
            if (from.HasValue) query = query.Where(p => p.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(p => p.CreatedAt <= to.Value);
            return query;
        }

        /// <summary>Whitelist — the ONLY values ever bound into the date_trunc(...) SQL parameter.</summary>
        private static string NormalizeGranularity(string? granularity) => granularity?.Trim().ToLowerInvariant() switch
        {
            "week" => "week",
            "month" => "month",
            "year" => "year",
            _ => "day"
        };

        private static string FormatPeriodLabel(DateTime periodStart, string granularity) => granularity switch
        {
            "week" => $"{periodStart:yyyy}-W{System.Globalization.ISOWeek.GetWeekOfYear(periodStart):D2}",
            "month" => periodStart.ToString("yyyy-MM"),
            "year" => periodStart.ToString("yyyy"),
            _ => periodStart.ToString("yyyy-MM-dd")
        };

        private static decimal Percentage(int part, int total)
            => total == 0 ? 0m : Math.Round((decimal)part / total * 100m, 2);

        /// <summary>Ad-hoc projection for the raw date_trunc chart query — not an EF entity.</summary>
        private sealed class RevenueChartRow
        {
            public DateTime PeriodStart { get; set; }
            public decimal GrossRevenue { get; set; }
            public decimal PlatformRevenue { get; set; }
            public decimal CoachRevenue { get; set; }
            public int TransactionCount { get; set; }
        }
    }
}
