using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Tests.Reviews;

internal sealed class FakeReviewRepository : IReviewRepository
{
    public Review? ExistingPair;                         // GetByCoachAndLearner*
    public readonly Dictionary<Guid, Review> ById = new();
    public List<Review> PagedItems = new();
    public bool HasSuccessful = true;
    public bool HasNonExpired = true;
    public int RecalcCount;
    public CoachRatingStats Stats = new();

    public void Seed(Review review) => ById[review.Id] = review;

    public Task<Review?> GetByIdAsync(Guid id)
        => Task.FromResult(ById.TryGetValue(id, out var r) ? r : null);

    public Task<Review?> GetByIdForUpdateAsync(Guid id)
        => Task.FromResult(ById.TryGetValue(id, out var r) ? r : null);

    public Task<Review?> GetByCoachAndLearnerAsync(Guid coachId, Guid learnerId)
        => Task.FromResult(ExistingPair);

    public Task<Review?> GetByCoachAndLearnerForUpdateAsync(Guid coachId, Guid learnerId)
        => Task.FromResult(ExistingPair);

    public Task<(List<Review> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, ReviewFilterRequest filter)
        => Task.FromResult((PagedItems, PagedItems.Count));

    public Task AddAsync(Review review)
    {
        ById[review.Id] = review;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;

    public Task<CoachRatingStats> GetRatingStatsByCoachAsync(Guid coachId) => Task.FromResult(Stats);

    public Task<bool> HasSuccessfulBookingForReviewAsync(Guid learnerId, Guid coachId, Guid? bookingId)
        => Task.FromResult(HasSuccessful);

    public Task<bool> HasNonExpiredSuccessfulBookingAsync(Guid learnerId, Guid coachId)
        => Task.FromResult(HasNonExpired);

    public Task RecalculateCoachRatingAsync(Guid coachId)
    {
        RecalcCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeReviewReportRepository : IReviewReportRepository
{
    public readonly Dictionary<Guid, Report> ById = new();
    public readonly List<Report> Added = new();
    public Report? OpenReport;
    public List<Report> PagedItems = new();

    public void Seed(Report report) => ById[report.Id] = report;

    public Task<Report?> GetByIdAsync(Guid id)
        => Task.FromResult(ById.TryGetValue(id, out var r) ? r : null);

    public Task<Report?> GetByIdForUpdateAsync(Guid id)
        => Task.FromResult(ById.TryGetValue(id, out var r) ? r : null);

    public Task<Report?> GetOpenReportAsync(Guid reviewId, Guid reporterId)
        => Task.FromResult(OpenReport);

    public Task<(List<Report> Items, int TotalCount)> GetPagedReviewReportsAsync(ReviewReportFilterRequest filter)
        => Task.FromResult((PagedItems, PagedItems.Count));

    public Task AddAsync(Report report)
    {
        Added.Add(report);
        ById[report.Id] = report;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}

internal sealed class FakeReviewCoachRepository : ICoachRepository
{
    public bool Exists = true;

    public Task<bool> ExistsByUserIdAsync(Guid userId) => Task.FromResult(Exists);
    public Task<CoachProfile?> GetByUserIdAsync(Guid userId) => throw new NotImplementedException();
    public Task<CoachProfile?> GetByUserIdWithDetailsAsync(Guid userId) => throw new NotImplementedException();
    public Task<CoachProfile?> GetByUserIdForUpdateAsync(Guid userId) => throw new NotImplementedException();
    public Task SaveChangesAsync() => Task.CompletedTask;
    public Task CreateCoachProfileAsync(CoachProfile coachProfile, int coachRoleId, List<int> sportIds) => throw new NotImplementedException();
}
