using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Dashboard;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Read-only aggregate queries for the coach and admin dashboards. All counts/sums run as
    /// server-side EF aggregations (no in-memory materialisation of rows).
    /// </summary>
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CoachDashboardResponse> GetCoachDashboardAsync(
            Guid coachId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var bookings = ApplyCreatedRange(
                _context.Bookings.AsNoTracking().Where(b => b.CoachId == coachId), fromDate, toDate);

            var sessions = _context.TrainingSessions.AsNoTracking().Where(s => s.CoachId == coachId);
            if (fromDate.HasValue) sessions = sessions.Where(s => s.StartTime >= fromDate.Value);
            if (toDate.HasValue) sessions = sessions.Where(s => s.StartTime <= toDate.Value);

            var withdrawals = ApplyCreatedRange(
                _context.WithdrawalRequests.AsNoTracking().Where(w => w.CoachId == coachId), fromDate, toDate);

            var now = DateTime.UtcNow;
            var result = new CoachDashboardResponse { CoachId = coachId };

            result.ActiveBookings = await bookings.CountAsync(b => b.Status == BookingStatuses.Active);
            result.CompletedBookings = await bookings.CountAsync(b => b.Status == BookingStatuses.Completed);
            result.CancelledBookings = await bookings.CountAsync(b => b.Status == BookingStatuses.Cancelled);

            result.RequestedSessions = await sessions.CountAsync(s => s.Status == TrainingSessionStatuses.Requested);
            result.UpcomingSessions = await sessions.CountAsync(s => s.Status == TrainingSessionStatuses.Scheduled && s.StartTime >= now);
            result.CompletedSessions = await sessions.CountAsync(s => s.Status == TrainingSessionStatuses.Completed);
            result.CancelledSessions = await sessions.CountAsync(s => s.Status == TrainingSessionStatuses.Cancelled);

            var finished = result.CompletedSessions + result.CancelledSessions;
            result.SessionCompletionRate = finished == 0
                ? 0m
                : Math.Round((decimal)result.CompletedSessions / finished, 4);

            var wallet = await _context.CoachWallets.AsNoTracking()
                .FirstOrDefaultAsync(w => w.CoachId == coachId);
            if (wallet != null)
            {
                result.TotalEarned = wallet.TotalEarned;
                result.AvailableBalance = wallet.AvailableBalance;
                result.PendingBalance = wallet.PendingBalance;
                result.TotalWithdrawn = wallet.TotalWithdrawn;
            }

            result.PendingWithdrawalRequests = await withdrawals.CountAsync(w =>
                w.Status == WithdrawalRequestStatuses.Pending ||
                w.Status == WithdrawalRequestStatuses.Processing);

            return result;
        }

        public async Task<AdminDashboardResponse> GetAdminDashboardAsync(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var bookings = ApplyCreatedRange(_context.Bookings.AsNoTracking(), fromDate, toDate);
            var withdrawals = ApplyCreatedRange(_context.WithdrawalRequests.AsNoTracking(), fromDate, toDate);

            var result = new AdminDashboardResponse
            {
                TotalUsers = await _context.Users.AsNoTracking().CountAsync(),
                TotalLearners = await _context.LearnerProfiles.AsNoTracking().CountAsync(),
                TotalCoaches = await _context.CoachProfiles.AsNoTracking().CountAsync(),
                PublishedPackages = await _context.TrainingPackages.AsNoTracking()
                    .CountAsync(p => p.Status == TrainingPackageStatuses.Published),

                TotalBookings = await bookings.CountAsync(),
                ActiveBookings = await bookings.CountAsync(b => b.Status == BookingStatuses.Active),
                CompletedBookings = await bookings.CountAsync(b => b.Status == BookingStatuses.Completed),
                CancelledBookings = await bookings.CountAsync(b => b.Status == BookingStatuses.Cancelled),

                PendingWithdrawals = await withdrawals.CountAsync(w => w.Status == WithdrawalRequestStatuses.Pending),
                ProcessingWithdrawals = await withdrawals.CountAsync(w => w.Status == WithdrawalRequestStatuses.Processing),
                PaidWithdrawals = await withdrawals.CountAsync(w => w.Status == WithdrawalRequestStatuses.Paid),
                FailedWithdrawals = await withdrawals.CountAsync(w => w.Status == WithdrawalRequestStatuses.Failed),
            };

            // Accounting over PAID bookings (active or completed with PaidAt set).
            var paidBookings = bookings.Where(b =>
                b.PaidAt != null &&
                (b.Status == BookingStatuses.Active || b.Status == BookingStatuses.Completed));

            result.GrossRevenue = await paidBookings.SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;
            result.PlatformFeeRevenue = await paidBookings.SumAsync(b => (decimal?)b.PlatformFeeAmount) ?? 0m;
            result.CoachPayable = await paidBookings.SumAsync(b => (decimal?)b.CoachReceiveAmount) ?? 0m;

            result.TotalWithdrawnPaid = await withdrawals
                .Where(w => w.Status == WithdrawalRequestStatuses.Paid)
                .SumAsync(w => (decimal?)w.Amount) ?? 0m;

            return result;
        }

        private static IQueryable<Booking> ApplyCreatedRange(IQueryable<Booking> query, DateTime? from, DateTime? to)
        {
            if (from.HasValue) query = query.Where(b => b.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(b => b.CreatedAt <= to.Value);
            return query;
        }

        private static IQueryable<WithdrawalRequest> ApplyCreatedRange(IQueryable<WithdrawalRequest> query, DateTime? from, DateTime? to)
        {
            if (from.HasValue) query = query.Where(w => w.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(w => w.CreatedAt <= to.Value);
            return query;
        }
    }
}
