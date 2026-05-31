using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Reviews;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Reviews;

/// <summary>Orchestration + authorization rules for the review service (fakes).</summary>
public class ReviewServiceTests
{
    private static readonly Guid CoachId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid LearnerId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");

    private static ReviewService Build(FakeReviewRepository reviews, FakeReviewCoachRepository? coaches = null)
        => new(
            reviews,
            coaches ?? new FakeReviewCoachRepository(),
            new CreateReviewRequestValidator(),
            new UpdateReviewRequestValidator(),
            new ReviewFilterRequestValidator());

    private static Review ActiveReview(Guid? learner = null) => new()
    {
        Id = Guid.NewGuid(),
        CoachId = CoachId,
        learner_id = learner ?? LearnerId,
        Rating = 4,
        Status = ReviewStatuses.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // Eligible learner can create, and creation recalculates coach rating (8).
    [Fact]
    public async Task Create_EligibleLearner_SucceedsAndRecalculates()
    {
        var reviews = new FakeReviewRepository { HasSuccessful = true };
        var service = Build(reviews);

        var result = await service.CreateAsync(LearnerId,
            new CreateReviewRequest { CoachId = CoachId, Rating = 5, Comment = "Great" });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data!.Rating);
        Assert.Equal(1, reviews.RecalcCount);
    }

    // 5/3/4. Ineligible learner (no successful booking) cannot create.
    [Fact]
    public async Task Create_NotEligible_ThrowsForbidden()
    {
        var reviews = new FakeReviewRepository { HasSuccessful = false };
        var service = Build(reviews);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(LearnerId, new CreateReviewRequest { CoachId = CoachId, Rating = 5 }));

        Assert.Equal(ErrorCodes.ReviewNotAllowed, ex.Code);
        Assert.Equal(0, reviews.RecalcCount);
    }

    // 6. Duplicate active review ⇒ 409.
    [Fact]
    public async Task Create_DuplicateActiveReview_ThrowsConflict()
    {
        var reviews = new FakeReviewRepository { HasSuccessful = true, ExistingPair = ActiveReview() };
        var service = Build(reviews);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(LearnerId, new CreateReviewRequest { CoachId = CoachId, Rating = 5 }));

        Assert.Equal(ErrorCodes.ReviewAlreadyExists, ex.Code);
    }

    // 7. Rating must be 1..5.
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task Create_InvalidRating_ThrowsValidation(int rating)
    {
        var service = Build(new FakeReviewRepository());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(LearnerId, new CreateReviewRequest { CoachId = CoachId, Rating = rating }));
    }

    // Cannot review yourself.
    [Fact]
    public async Task Create_SelfReview_ThrowsForbidden()
    {
        var service = Build(new FakeReviewRepository());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(CoachId, new CreateReviewRequest { CoachId = CoachId, Rating = 5 }));
    }

    // 9. Owner can update while a non-expired successful booking exists.
    [Fact]
    public async Task Update_OwnerNonExpired_Succeeds()
    {
        var review = ActiveReview();
        var reviews = new FakeReviewRepository { HasNonExpired = true };
        reviews.Seed(review);
        var service = Build(reviews);

        var result = await service.UpdateAsync(LearnerId, review.Id,
            new UpdateReviewRequest { Rating = 2, Comment = "Changed" });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, review.Rating);
        Assert.Equal(1, reviews.RecalcCount);
    }

    // 10. Owner cannot update after all successful bookings expired ⇒ 409.
    [Fact]
    public async Task Update_Expired_ThrowsConflict()
    {
        var review = ActiveReview();
        var reviews = new FakeReviewRepository { HasNonExpired = false };
        reviews.Seed(review);
        var service = Build(reviews);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(LearnerId, review.Id, new UpdateReviewRequest { Rating = 2 }));

        Assert.Equal(ErrorCodes.ReviewEditExpired, ex.Code);
    }

    // 11. Another learner cannot update someone else's review.
    [Fact]
    public async Task Update_NotOwner_ThrowsForbidden()
    {
        var review = ActiveReview();
        var reviews = new FakeReviewRepository { HasNonExpired = true };
        reviews.Seed(review);
        var service = Build(reviews);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateAsync(Guid.NewGuid(), review.Id, new UpdateReviewRequest { Rating = 1 }));

        Assert.Equal(ErrorCodes.ReviewNotOwned, ex.Code);
    }

    // 11. Another learner cannot delete someone else's review.
    [Fact]
    public async Task Delete_NotOwner_ThrowsForbidden()
    {
        var review = ActiveReview();
        var reviews = new FakeReviewRepository();
        reviews.Seed(review);
        var service = Build(reviews);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.DeleteOwnAsync(Guid.NewGuid(), review.Id));
    }

    // Owner soft-deletes and stats recalculated.
    [Fact]
    public async Task Delete_Owner_SoftDeletesAndRecalculates()
    {
        var review = ActiveReview();
        var reviews = new FakeReviewRepository();
        reviews.Seed(review);
        var service = Build(reviews);

        var result = await service.DeleteOwnAsync(LearnerId, review.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatuses.Deleted, review.Status);
        Assert.Equal(1, reviews.RecalcCount);
    }

    // 13. Summary maps stats + breakdown.
    [Fact]
    public async Task Summary_ReturnsAverageAndBreakdown()
    {
        var reviews = new FakeReviewRepository
        {
            Stats = new CoachRatingStats
            {
                TotalReviews = 4, AverageRating = 4.25m,
                FiveStar = 2, FourStar = 1, ThreeStar = 0, TwoStar = 0, OneStar = 1
            }
        };
        var service = Build(reviews);

        var result = await service.GetCoachReviewSummaryAsync(CoachId);

        Assert.Equal(4.25m, result.Data!.AverageRating);
        Assert.Equal(4, result.Data.TotalReviews);
        Assert.Equal(2, result.Data.RatingBreakdown.FiveStar);
        Assert.Equal(1, result.Data.RatingBreakdown.OneStar);
    }

    // Revive a previously self-deleted review instead of inserting a duplicate.
    [Fact]
    public async Task Create_AfterSelfDelete_RevivesSameRow()
    {
        var deleted = ActiveReview();
        deleted.Status = ReviewStatuses.Deleted;
        var reviews = new FakeReviewRepository { HasSuccessful = true, ExistingPair = deleted };
        reviews.Seed(deleted);
        var service = Build(reviews);

        var result = await service.CreateAsync(LearnerId,
            new CreateReviewRequest { CoachId = CoachId, Rating = 4 });

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatuses.Active, deleted.Status);
        Assert.Equal(4, deleted.Rating);
    }
}
