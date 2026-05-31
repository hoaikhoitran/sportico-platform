using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Core.Entities;
using SporticoApp.Infrastructure.Persistence;
using SporticoApp.Infrastructure.Persistence.Repositories;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Reviews;

/// <summary>
/// Eligibility / stats / recalculation rules tested against a real EF model (InMemory),
/// because the booking-eligibility logic lives in the repository query.
/// </summary>
public class ReviewRepositoryTests
{
    private static readonly Guid CoachId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid LearnerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    private static AppDbContext NewContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Booking Booking(string status, DateTime? paidAt, DateTime? expiresAt = null,
        Guid? coachId = null, Guid? learnerId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            LearnerId = learnerId ?? LearnerId,
            CoachId = coachId ?? CoachId,
            TrainingPackageId = Guid.NewGuid(),
            Status = status,
            PaidAt = paidAt,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static Review Review(int rating, string status = ReviewStatuses.Active,
        Guid? coachId = null, Guid? learnerId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            CoachId = coachId ?? CoachId,
            learner_id = learnerId ?? LearnerId,
            Rating = rating,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    // 1 & 2. Active or completed + paid booking ⇒ eligible.
    [Theory]
    [InlineData(BookingStatuses.Active)]
    [InlineData(BookingStatuses.Completed)]
    public async Task HasSuccessfulBooking_ActiveOrCompletedPaid_True(string status)
    {
        await using var ctx = NewContext();
        ctx.Bookings.Add(Booking(status, paidAt: DateTime.UtcNow.AddDays(-1)));
        await ctx.SaveChangesAsync();
        var repo = new ReviewRepository(ctx);

        Assert.True(await repo.HasSuccessfulBookingForReviewAsync(LearnerId, CoachId, null));
    }

    // 3 & 4. pending_payment / cancelled / refunded ⇒ not eligible.
    [Theory]
    [InlineData(BookingStatuses.PendingPayment)]
    [InlineData(BookingStatuses.Cancelled)]
    [InlineData(BookingStatuses.Refunded)]
    public async Task HasSuccessfulBooking_NonSuccessfulStatus_False(string status)
    {
        await using var ctx = NewContext();
        // Even with PaidAt set, a non-active/completed status is not "successful".
        ctx.Bookings.Add(Booking(status, paidAt: DateTime.UtcNow.AddDays(-1)));
        await ctx.SaveChangesAsync();
        var repo = new ReviewRepository(ctx);

        Assert.False(await repo.HasSuccessfulBookingForReviewAsync(LearnerId, CoachId, null));
    }

    // Active but unpaid (PaidAt null) ⇒ not eligible.
    [Fact]
    public async Task HasSuccessfulBooking_ActiveButUnpaid_False()
    {
        await using var ctx = NewContext();
        ctx.Bookings.Add(Booking(BookingStatuses.Active, paidAt: null));
        await ctx.SaveChangesAsync();
        var repo = new ReviewRepository(ctx);

        Assert.False(await repo.HasSuccessfulBookingForReviewAsync(LearnerId, CoachId, null));
    }

    // 5. Never bought from this coach ⇒ not eligible.
    [Fact]
    public async Task HasSuccessfulBooking_NoBooking_False()
    {
        await using var ctx = NewContext();
        var repo = new ReviewRepository(ctx);

        Assert.False(await repo.HasSuccessfulBookingForReviewAsync(LearnerId, CoachId, null));
    }

    // Specific bookingId must belong to the learner+coach.
    [Fact]
    public async Task HasSuccessfulBooking_WrongBookingId_False()
    {
        await using var ctx = NewContext();
        var mine = Booking(BookingStatuses.Active, DateTime.UtcNow.AddDays(-1));
        ctx.Bookings.Add(mine);
        await ctx.SaveChangesAsync();
        var repo = new ReviewRepository(ctx);

        Assert.True(await repo.HasSuccessfulBookingForReviewAsync(LearnerId, CoachId, mine.Id));
        Assert.False(await repo.HasSuccessfulBookingForReviewAsync(LearnerId, CoachId, Guid.NewGuid()));
    }

    // Edit eligibility: non-expired (future / null) ⇒ true, past ⇒ false.
    [Fact]
    public async Task HasNonExpiredSuccessfulBooking_RespectsExpiresAt()
    {
        await using var ctx = NewContext();
        ctx.Bookings.Add(Booking(BookingStatuses.Active, DateTime.UtcNow.AddDays(-10),
            expiresAt: DateTime.UtcNow.AddDays(20)));
        await ctx.SaveChangesAsync();
        var repo = new ReviewRepository(ctx);
        Assert.True(await repo.HasNonExpiredSuccessfulBookingAsync(LearnerId, CoachId));

        await using var ctx2 = NewContext();
        ctx2.Bookings.Add(Booking(BookingStatuses.Completed, DateTime.UtcNow.AddDays(-40),
            expiresAt: DateTime.UtcNow.AddDays(-1)));
        await ctx2.SaveChangesAsync();
        var repo2 = new ReviewRepository(ctx2);
        Assert.False(await repo2.HasNonExpiredSuccessfulBookingAsync(LearnerId, CoachId));
    }

    // 13. Stats: only active reviews counted; breakdown + average correct.
    [Fact]
    public async Task GetRatingStats_OnlyActiveCounted()
    {
        await using var ctx = NewContext();
        ctx.Reviews.AddRange(
            Review(5),
            Review(5, learnerId: Guid.NewGuid()),
            Review(3, learnerId: Guid.NewGuid()),
            Review(1, ReviewStatuses.Hidden, learnerId: Guid.NewGuid()),   // excluded
            Review(1, ReviewStatuses.Deleted, learnerId: Guid.NewGuid())); // excluded
        await ctx.SaveChangesAsync();
        var repo = new ReviewRepository(ctx);

        var stats = await repo.GetRatingStatsByCoachAsync(CoachId);

        Assert.Equal(3, stats.TotalReviews);
        Assert.Equal(2, stats.FiveStar);
        Assert.Equal(1, stats.ThreeStar);
        Assert.Equal(0, stats.OneStar);
        Assert.Equal(Math.Round((5 + 5 + 3) / 3m, 2), stats.AverageRating);
    }

    // 12. Paged listing returns active only.
    [Fact]
    public async Task GetPagedByCoach_ReturnsActiveOnly()
    {
        await using var ctx = NewContext();
        // The listing Includes Coach.User + learner (required navs); seed those rows so the
        // InMemory provider materializes the active review.
        ctx.Users.Add(new User { Id = CoachId, FullName = "Coach", Email = "coach@test.io", PasswordHash = "x", Status = "active" });
        ctx.Users.Add(new User { Id = LearnerId, FullName = "Learner", Email = "learner@test.io", PasswordHash = "x", Status = "active" });
        ctx.CoachProfiles.Add(new CoachProfile
        {
            UserId = CoachId, Rating = 0, TotalReviews = 0,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        ctx.Reviews.AddRange(
            Review(5),
            Review(4, ReviewStatuses.Hidden, learnerId: Guid.NewGuid()),
            Review(2, ReviewStatuses.Deleted, learnerId: Guid.NewGuid()));
        await ctx.SaveChangesAsync();
        var repo = new ReviewRepository(ctx);

        var (items, total) = await repo.GetPagedByCoachAsync(CoachId,
            new ReviewFilterRequest { PageNumber = 1, PageSize = 10 });

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal(ReviewStatuses.Active, items[0].Status);
    }

    // 8 & 18. Recalculate writes average + total to CoachProfile and excludes non-active.
    [Fact]
    public async Task RecalculateCoachRating_UpdatesCoachProfile()
    {
        await using var ctx = NewContext();
        ctx.CoachProfiles.Add(new CoachProfile
        {
            UserId = CoachId,
            Rating = 0,
            TotalReviews = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        ctx.Reviews.AddRange(
            Review(4),
            Review(2, learnerId: Guid.NewGuid()),
            Review(1, ReviewStatuses.Hidden, learnerId: Guid.NewGuid()));
        await ctx.SaveChangesAsync();
        var repo = new ReviewRepository(ctx);

        await repo.RecalculateCoachRatingAsync(CoachId);

        var coach = await ctx.CoachProfiles.AsNoTracking().FirstAsync(c => c.UserId == CoachId);
        Assert.Equal(2, coach.TotalReviews);
        Assert.Equal(3.00m, coach.Rating); // (4+2)/2, hidden excluded
    }
}
