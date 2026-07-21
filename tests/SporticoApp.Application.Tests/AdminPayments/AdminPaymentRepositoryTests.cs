using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SporticoApp.Application.Options;
using SporticoApp.Core.Entities;
using SporticoApp.Infrastructure.Persistence;
using SporticoApp.Infrastructure.Persistence.Repositories;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.AdminPayments;

/// <summary>
/// Payment total reconciliation, tested against the real EF model (InMemory) — the same technique
/// DashboardRepositoryTests already uses. Only GetStatisticsAsync is covered here (pure LINQ, no
/// raw SQL); GetRevenueChartAsync uses Postgres-only raw SQL (date_trunc) and cannot run against the
/// InMemory provider — that path was instead verified against a real Postgres instance (see the
/// audit report for the executed evidence: schema creation, VN-boundary bucketing, and a live API
/// round trip all produced reconciling numbers).
/// </summary>
public class AdminPaymentRepositoryTests
{
    private static readonly Guid Coach = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid Learner = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static AppDbContext NewContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Booking PaidBooking(decimal total, decimal platformFee, decimal coachReceive, string status = "active")
        => new()
        {
            Id = Guid.NewGuid(),
            CoachId = Coach,
            LearnerId = Learner,
            TrainingPackageId = Guid.NewGuid(),
            Status = status,
            TotalAmount = total,
            PlatformFeeAmount = platformFee,
            CoachReceiveAmount = coachReceive,
            PaidAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

    private static Payment PaymentFor(string status, decimal amount, Guid? bookingRef = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = Learner,
            Amount = amount,
            Method = PaymentMethods.PayOs,
            Status = status,
            ReferenceType = PaymentReferenceTypes.Booking,
            ReferenceId = bookingRef ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            PaidAt = status == PaymentStatuses.Paid ? DateTime.UtcNow.AddDays(-1) : null
        };

    private static AdminPaymentRepository BuildRepository(AppDbContext ctx)
        => new(ctx);

    // 1. TotalRevenue must equal PlatformRevenue + CoachRevenue (the fee split is a full partition
    // of the total — no money unaccounted for).
    [Fact]
    public async Task GetStatistics_TotalRevenue_ReconcilesWithPlatformPlusCoachRevenue()
    {
        await using var ctx = NewContext();
        ctx.Bookings.AddRange(
            PaidBooking(total: 1_000_000m, platformFee: 150_000m, coachReceive: 850_000m),
            PaidBooking(total: 500_000m, platformFee: 0m, coachReceive: 500_000m), // 0% commission booking
            PaidBooking(total: 300_000m, platformFee: 45_000m, coachReceive: 255_000m, status: "completed"));
        await ctx.SaveChangesAsync();

        var repo = BuildRepository(ctx);
        var stats = await repo.GetStatisticsAsync(null, null);

        Assert.Equal(1_800_000m, stats.TotalRevenue);
        Assert.Equal(195_000m, stats.PlatformRevenue);
        Assert.Equal(1_605_000m, stats.CoachRevenue);
        Assert.Equal(stats.TotalRevenue, stats.PlatformRevenue + stats.CoachRevenue); // the reconciliation itself
    }

    // 2. Only paid bookings (active/completed with PaidAt set) count — pending/cancelled bookings
    // must not inflate revenue, matching DashboardRepository's own rule exactly.
    [Fact]
    public async Task GetStatistics_ExcludesUnpaidAndCancelledBookings()
    {
        await using var ctx = NewContext();
        ctx.Bookings.AddRange(
            PaidBooking(total: 1_000_000m, platformFee: 100_000m, coachReceive: 900_000m),
            new Booking
            {
                Id = Guid.NewGuid(), CoachId = Coach, LearnerId = Learner, TrainingPackageId = Guid.NewGuid(),
                Status = BookingStatuses.PendingPayment, TotalAmount = 999_999m, PaidAt = null,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new Booking
            {
                Id = Guid.NewGuid(), CoachId = Coach, LearnerId = Learner, TrainingPackageId = Guid.NewGuid(),
                Status = BookingStatuses.Cancelled, TotalAmount = 999_999m, PaidAt = null,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
        await ctx.SaveChangesAsync();

        var stats = await BuildRepository(ctx).GetStatisticsAsync(null, null);

        Assert.Equal(1_000_000m, stats.TotalRevenue); // only the one genuinely paid booking
    }

    // 3. Transaction counts partition correctly: Total = Successful + Failed + Pending (+ Refunded,
    // 0 today since no refund flow exists yet).
    [Fact]
    public async Task GetStatistics_TransactionCounts_Reconcile()
    {
        await using var ctx = NewContext();
        ctx.Payments.AddRange(
            PaymentFor(PaymentStatuses.Paid, 100_000m),
            PaymentFor(PaymentStatuses.Paid, 200_000m),
            PaymentFor(PaymentStatuses.Pending, 50_000m),
            PaymentFor(PaymentStatuses.Failed, 75_000m),
            PaymentFor(PaymentStatuses.Cancelled, 25_000m));
        await ctx.SaveChangesAsync();

        var stats = await BuildRepository(ctx).GetStatisticsAsync(null, null);

        Assert.Equal(5, stats.TotalTransactions);
        Assert.Equal(2, stats.SuccessfulTransactions);
        Assert.Equal(1, stats.PendingTransactions);
        Assert.Equal(2, stats.FailedTransactions); // failed + cancelled folded together
        Assert.Equal(0, stats.RefundedTransactions);
        Assert.Equal(
            stats.TotalTransactions,
            stats.SuccessfulTransactions + stats.FailedTransactions + stats.PendingTransactions + stats.RefundedTransactions);
    }

    // 4. AverageTransactionValue is computed only over successful (paid) payments.
    [Fact]
    public async Task GetStatistics_AverageTransactionValue_OnlyOverPaidPayments()
    {
        await using var ctx = NewContext();
        ctx.Payments.AddRange(
            PaymentFor(PaymentStatuses.Paid, 100_000m),
            PaymentFor(PaymentStatuses.Paid, 300_000m),
            PaymentFor(PaymentStatuses.Failed, 999_999m)); // must NOT skew the average
        await ctx.SaveChangesAsync();

        var stats = await BuildRepository(ctx).GetStatisticsAsync(null, null);

        Assert.Equal(200_000m, stats.AverageTransactionValue); // (100k + 300k) / 2
    }

    // 5. Date-range filter is applied (bookings/payments outside range excluded).
    [Fact]
    public async Task GetStatistics_DateRangeFilter_ExcludesOutOfRangeRows()
    {
        await using var ctx = NewContext();
        var inRange = PaidBooking(total: 1_000_000m, platformFee: 100_000m, coachReceive: 900_000m);
        inRange.CreatedAt = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);

        var outOfRange = PaidBooking(total: 5_000_000m, platformFee: 500_000m, coachReceive: 4_500_000m);
        outOfRange.CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ctx.Bookings.AddRange(inRange, outOfRange);
        await ctx.SaveChangesAsync();

        var stats = await BuildRepository(ctx).GetStatisticsAsync(
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1_000_000m, stats.TotalRevenue);
    }
}
