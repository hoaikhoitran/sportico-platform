using Microsoft.EntityFrameworkCore;
using SporticoApp.Core.Entities;
using SporticoApp.Infrastructure.Persistence;
using SporticoApp.Infrastructure.Persistence.Repositories;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Dashboard;

/// <summary>
/// Part K: dashboard aggregate calculations, tested against the real EF model (InMemory).
/// </summary>
public class DashboardRepositoryTests
{
    private static readonly Guid Coach = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherCoach = Guid.Parse("c0000000-0000-0000-0000-000000000002");
    private static readonly Guid Learner = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static AppDbContext NewContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Booking Booking(Guid coach, string status, decimal total = 0, decimal fee = 0, decimal coachReceive = 0, bool paid = false)
        => new()
        {
            Id = Guid.NewGuid(),
            CoachId = coach,
            LearnerId = Learner,
            TrainingPackageId = Guid.NewGuid(),
            Status = status,
            TotalAmount = total,
            PlatformFeeAmount = fee,
            CoachReceiveAmount = coachReceive,
            PaidAt = paid ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static TrainingSession Session(string status, DateTime start)
        => new()
        {
            Id = Guid.NewGuid(),
            CoachId = Coach,
            LearnerId = Learner,
            BookingId = Guid.NewGuid(),
            Status = status,
            StartTime = start,
            EndTime = start.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static WithdrawalRequest Withdrawal(Guid coach, string status, decimal amount)
        => new()
        {
            Id = Guid.NewGuid(),
            CoachId = coach,
            CoachWalletId = Guid.NewGuid(),
            Status = status,
            Amount = amount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    [Fact]
    public async Task CoachDashboard_AggregatesCorrectly()
    {
        await using var ctx = NewContext();
        ctx.Bookings.AddRange(
            Booking(Coach, BookingStatuses.Active),
            Booking(Coach, BookingStatuses.Completed),
            Booking(Coach, BookingStatuses.Cancelled),
            Booking(OtherCoach, BookingStatuses.Active)); // excluded (other coach)
        ctx.TrainingSessions.AddRange(
            Session(TrainingSessionStatuses.Requested, DateTime.UtcNow.AddDays(2)),
            Session(TrainingSessionStatuses.Scheduled, DateTime.UtcNow.AddDays(3)),  // upcoming
            Session(TrainingSessionStatuses.Scheduled, DateTime.UtcNow.AddDays(-3)), // not upcoming (past)
            Session(TrainingSessionStatuses.Completed, DateTime.UtcNow.AddDays(-1)),
            Session(TrainingSessionStatuses.Cancelled, DateTime.UtcNow.AddDays(-1)));
        ctx.CoachWallets.Add(new CoachWallet
        {
            Id = Guid.NewGuid(), CoachId = Coach,
            TotalEarned = 1000m, AvailableBalance = 600m, PendingBalance = 200m, TotalWithdrawn = 200m
        });
        ctx.WithdrawalRequests.AddRange(
            Withdrawal(Coach, WithdrawalRequestStatuses.Pending, 100m),
            Withdrawal(Coach, WithdrawalRequestStatuses.Processing, 100m),
            Withdrawal(Coach, WithdrawalRequestStatuses.Paid, 100m));
        await ctx.SaveChangesAsync();

        var d = await new DashboardRepository(ctx).GetCoachDashboardAsync(Coach, null, null);

        Assert.Equal(1, d.ActiveBookings);
        Assert.Equal(1, d.CompletedBookings);
        Assert.Equal(1, d.CancelledBookings);
        Assert.Equal(1, d.RequestedSessions);
        Assert.Equal(1, d.UpcomingSessions);   // only the future scheduled one
        Assert.Equal(1, d.CompletedSessions);
        Assert.Equal(1, d.CancelledSessions);
        Assert.Equal(0.5m, d.SessionCompletionRate); // 1 / (1 + 1)
        Assert.Equal(1000m, d.TotalEarned);
        Assert.Equal(600m, d.AvailableBalance);
        Assert.Equal(200m, d.PendingBalance);
        Assert.Equal(200m, d.TotalWithdrawn);
        Assert.Equal(2, d.PendingWithdrawalRequests); // pending + processing
    }

    [Fact]
    public async Task CoachDashboard_NoData_ReturnsZeros()
    {
        await using var ctx = NewContext();
        var d = await new DashboardRepository(ctx).GetCoachDashboardAsync(Coach, null, null);
        Assert.Equal(0, d.ActiveBookings);
        Assert.Equal(0m, d.SessionCompletionRate);
        Assert.Equal(0m, d.AvailableBalance); // no wallet => 0
    }

    [Fact]
    public async Task AdminDashboard_AggregatesAccountingCorrectly()
    {
        await using var ctx = NewContext();
        for (var i = 0; i < 3; i++)
            ctx.Users.Add(new User { Id = Guid.NewGuid(), FullName = "U", Email = $"u{i}@t.io", PasswordHash = "x", Status = "active" });
        ctx.CoachProfiles.AddRange(
            new CoachProfile { UserId = Coach, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new CoachProfile { UserId = OtherCoach, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        ctx.LearnerProfiles.AddRange(
            new LearnerProfile { UserId = Learner, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new LearnerProfile { UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        ctx.TrainingPackages.AddRange(
            Package(TrainingPackageStatuses.Published),
            Package(TrainingPackageStatuses.Published),
            Package(TrainingPackageStatuses.Pending));
        ctx.Bookings.AddRange(
            Booking(Coach, BookingStatuses.Active, total: 1000m, fee: 150m, coachReceive: 850m, paid: true),
            Booking(Coach, BookingStatuses.Completed, total: 2000m, fee: 300m, coachReceive: 1700m, paid: true),
            Booking(Coach, BookingStatuses.Cancelled, total: 500m)); // not paid => excluded from revenue
        ctx.WithdrawalRequests.AddRange(
            Withdrawal(Coach, WithdrawalRequestStatuses.Pending, 100m),
            Withdrawal(Coach, WithdrawalRequestStatuses.Processing, 100m),
            Withdrawal(Coach, WithdrawalRequestStatuses.Paid, 500m),
            Withdrawal(Coach, WithdrawalRequestStatuses.Paid, 300m),
            Withdrawal(Coach, WithdrawalRequestStatuses.Failed, 100m));
        await ctx.SaveChangesAsync();

        var d = await new DashboardRepository(ctx).GetAdminDashboardAsync(null, null);

        Assert.Equal(3, d.TotalUsers);
        Assert.Equal(2, d.TotalLearners);
        Assert.Equal(2, d.TotalCoaches);
        Assert.Equal(2, d.PublishedPackages);
        Assert.Equal(3, d.TotalBookings);
        Assert.Equal(1, d.ActiveBookings);
        Assert.Equal(1, d.CompletedBookings);
        Assert.Equal(1, d.CancelledBookings);
        Assert.Equal(3000m, d.GrossRevenue);       // 1000 + 2000 (paid only)
        Assert.Equal(450m, d.PlatformFeeRevenue);  // 150 + 300
        Assert.Equal(2550m, d.CoachPayable);       // 850 + 1700
        Assert.Equal(1, d.PendingWithdrawals);
        Assert.Equal(1, d.ProcessingWithdrawals);
        Assert.Equal(2, d.PaidWithdrawals);
        Assert.Equal(1, d.FailedWithdrawals);
        Assert.Equal(800m, d.TotalWithdrawnPaid);  // 500 + 300
    }

    private static TrainingPackage Package(string status)
        => new()
        {
            Id = Guid.NewGuid(),
            CoachId = Coach,
            Title = "P",
            Status = status,
            SessionCount = 3,
            DurationDays = 30,
            Price = 1000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
