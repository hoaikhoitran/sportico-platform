using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Reviews;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Reviews;

/// <summary>Coach report + admin moderation flow.</summary>
public class ReviewReportServiceTests
{
    private static readonly Guid CoachId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
    private static readonly Guid OtherCoachId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002");
    private static readonly Guid LearnerId = Guid.Parse("ffffffff-0000-0000-0000-000000000001");
    private static readonly Guid AdminId = Guid.Parse("99999999-0000-0000-0000-000000000001");

    private static ReviewReportService Build(FakeReviewReportRepository reports, FakeReviewRepository reviews)
        => new(reports, reviews,
            new CreateReviewReportRequestValidator(),
            new ResolveReviewReportRequestValidator());

    private static Review ReviewFor(Guid coachId, string status = ReviewStatuses.Active) => new()
    {
        Id = Guid.NewGuid(),
        CoachId = coachId,
        learner_id = LearnerId,
        Rating = 1,
        Comment = "bad",
        Status = status,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Report ReportFor(Guid reviewId, Guid reporterId) => new()
    {
        Id = Guid.NewGuid(),
        reporter_id = reporterId,
        TargetType = ReportTargetTypes.Review,
        TargetId = reviewId,
        Reason = "spam",
        Status = ReportStatuses.Pending,
        ActionTaken = ReportActions.None,
        CreatedAt = DateTime.UtcNow
    };

    // 14. Coach can report a review written about themselves.
    [Fact]
    public async Task Report_OwnReview_Succeeds()
    {
        var review = ReviewFor(CoachId);
        var reviews = new FakeReviewRepository();
        reviews.Seed(review);
        var reports = new FakeReviewReportRepository();
        var service = Build(reports, reviews);

        var result = await service.ReportAsync(CoachId, review.Id,
            new CreateReviewReportRequest { Reason = "Defamatory", Description = "untrue" });

        Assert.True(result.IsSuccess);
        var created = Assert.Single(reports.Added);
        Assert.Equal(ReportTargetTypes.Review, created.TargetType);
        Assert.Equal(review.Id, created.TargetId);
        Assert.Equal(ReportStatuses.Pending, created.Status);
    }

    // 15. Coach cannot report a review about another coach.
    [Fact]
    public async Task Report_AnotherCoachReview_ThrowsForbidden()
    {
        var review = ReviewFor(OtherCoachId);
        var reviews = new FakeReviewRepository();
        reviews.Seed(review);
        var service = Build(new FakeReviewReportRepository(), reviews);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.ReportAsync(CoachId, review.Id, new CreateReviewReportRequest { Reason = "x" }));

        Assert.Equal(ErrorCodes.ReviewReportNotAllowed, ex.Code);
    }

    // 16. Admin rejects the report — review stays active, no recalculation.
    [Fact]
    public async Task Resolve_Reject_KeepsReviewActive()
    {
        var review = ReviewFor(CoachId);
        var report = ReportFor(review.Id, CoachId);
        var reviews = new FakeReviewRepository();
        reviews.Seed(review);
        var reports = new FakeReviewReportRepository();
        reports.Seed(report);
        var service = Build(reports, reviews);

        var result = await service.ResolveAsync(AdminId, report.Id,
            new ResolveReviewReportRequest { IsValid = false, ResolutionNote = "looks fine" });

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatuses.Rejected, report.Status);
        Assert.Equal(ReviewStatuses.Active, review.Status);
        Assert.Equal(0, reviews.RecalcCount);
        Assert.Equal(AdminId, report.HandledByUserId);
    }

    // 17 & 18. Admin accepts the report — review hidden, stats recalculated.
    [Fact]
    public async Task Resolve_ValidAndHide_HidesReviewAndRecalculates()
    {
        var review = ReviewFor(CoachId);
        var report = ReportFor(review.Id, CoachId);
        var reviews = new FakeReviewRepository();
        reviews.Seed(review);
        var reports = new FakeReviewReportRepository();
        reports.Seed(report);
        var service = Build(reports, reviews);

        var result = await service.ResolveAsync(AdminId, report.Id,
            new ResolveReviewReportRequest { IsValid = true, HideOrDeleteReview = true, ResolutionNote = "policy violation" });

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatuses.Resolved, report.Status);
        Assert.Equal(ReportActions.ReviewHidden, report.ActionTaken);
        Assert.Equal(ReviewStatuses.Hidden, review.Status);
        Assert.Equal("policy violation", review.ModerationReason);
        Assert.Equal(AdminId, review.DeletedByUserId);
        Assert.Equal(1, reviews.RecalcCount);
    }

    // Valid report but admin chooses NOT to hide — review stays, report resolved.
    [Fact]
    public async Task Resolve_ValidNoHide_KeepsReviewButResolves()
    {
        var review = ReviewFor(CoachId);
        var report = ReportFor(review.Id, CoachId);
        var reviews = new FakeReviewRepository();
        reviews.Seed(review);
        var reports = new FakeReviewReportRepository();
        reports.Seed(report);
        var service = Build(reports, reviews);

        await service.ResolveAsync(AdminId, report.Id,
            new ResolveReviewReportRequest { IsValid = true, HideOrDeleteReview = false });

        Assert.Equal(ReportStatuses.Resolved, report.Status);
        Assert.Equal(ReviewStatuses.Active, review.Status);
        Assert.Equal(ReportActions.None, report.ActionTaken);
        Assert.Equal(0, reviews.RecalcCount);
    }

    // Already-handled report cannot be resolved again.
    [Fact]
    public async Task Resolve_AlreadyHandled_ThrowsConflict()
    {
        var review = ReviewFor(CoachId);
        var report = ReportFor(review.Id, CoachId);
        report.Status = ReportStatuses.Resolved;
        var reviews = new FakeReviewRepository();
        reviews.Seed(review);
        var reports = new FakeReviewReportRepository();
        reports.Seed(report);
        var service = Build(reports, reviews);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ResolveAsync(AdminId, report.Id, new ResolveReviewReportRequest { IsValid = false }));
    }
}
