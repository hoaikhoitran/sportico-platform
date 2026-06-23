using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.TrainingPackages;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using Xunit;
using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

namespace SporticoApp.Application.Tests.TrainingPackages;

/// <summary>
/// Training-package creation with the new start/end-date + fixed-schedule model.
/// Covers: valid create persists the full schedule; schedule-size mismatch; out-of-range session;
/// non-positive slot count.
/// </summary>
public class TrainingPackageCreateTests
{
    private static readonly Guid CoachId = Guid.Parse("c0000000-0000-0000-0000-000000000010");
    private static readonly DateTime Start = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

    private static CreateTrainingPackageRequest ValidRequest(int sessionCount = 3)
    {
        var sessions = new List<CreateTrainingPackageSessionRequest>();
        for (var i = 1; i <= sessionCount; i++)
        {
            // One session per day at 09:00 → guaranteed in-range and non-overlapping.
            var start = Start.AddDays(i).AddHours(9);
            sessions.Add(new CreateTrainingPackageSessionRequest
            {
                SessionNumber = i,
                StartTime = start,
                EndTime = start.AddHours(1),
                MaxParticipants = 4,
                Location = "Court A",
                IsOnline = false
            });
        }

        return new CreateTrainingPackageRequest
        {
            SportId = 1,
            Title = "Beginner Tennis",
            Price = 1000,
            SessionCount = sessionCount,
            StartDate = Start,
            EndDate = End,
            Location = "Court A",
            Sessions = sessions
        };
    }

    private static (TrainingPackageService Svc, FakeTpRepo Repo) Build()
    {
        var repo = new FakeTpRepo();
        var svc = new TrainingPackageService(
            repo,
            new FakeCoachRepo(),
            new FakeSportRepo(),
            new CreateTrainingPackageRequestValidator(),
            new UpdateTrainingPackageRequestValidator(),
            new TrainingPackageFilterRequestValidator());
        return (svc, repo);
    }

    // 1. Coach can create a package with a valid date range, session count and full schedule.
    [Fact]
    public async Task Create_ValidSchedule_PersistsPackageWithSlots()
    {
        var (svc, repo) = Build();

        var result = await svc.CreateAsync(CoachId, ValidRequest(3));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data!.Sessions.Count);

        var saved = repo.Added!;
        Assert.Equal(TrainingPackageStatuses.Pending, saved.Status);
        Assert.Equal(3, saved.SessionSlots.Count);
        Assert.Equal(Start, saved.StartDate);
        Assert.Equal(End, saved.EndDate);
        Assert.Equal(31, saved.DurationDays); // derived from start/end inclusive
        Assert.Equal(new[] { 1, 2, 3 }, saved.SessionSlots.OrderBy(s => s.SessionNumber).Select(s => s.SessionNumber).ToArray());
        Assert.All(saved.SessionSlots, s =>
        {
            Assert.Equal(TrainingPackageSessionSlotStatuses.Open, s.Status);
            Assert.Equal(0, s.BookedParticipants);
            Assert.True(s.MaxParticipants > 0);
        });
    }

    // 2. Create fails when sessions.Count != sessionCount.
    [Fact]
    public async Task Create_SessionsCountMismatch_ThrowsValidation()
    {
        var (svc, _) = Build();
        var request = ValidRequest(3);
        request.SessionCount = 4; // schedule still has 3

        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(CoachId, request));
    }

    // 3. Create fails when a session falls outside [StartDate, EndDate].
    [Fact]
    public async Task Create_SessionOutsideRange_ThrowsValidation()
    {
        var (svc, _) = Build();
        var request = ValidRequest(3);
        var outOfRange = End.AddDays(5).AddHours(9); // after EndDate
        request.Sessions[2].StartTime = outOfRange;
        request.Sessions[2].EndTime = outOfRange.AddHours(1);

        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(CoachId, request));
    }

    // 4. Create fails when a session slot count (MaxParticipants) is not positive.
    [Fact]
    public async Task Create_SlotCountNotPositive_ThrowsValidation()
    {
        var (svc, _) = Build();
        var request = ValidRequest(3);
        request.Sessions[0].MaxParticipants = 0;

        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(CoachId, request));
    }

    // Extra: duplicate session numbers are rejected.
    [Fact]
    public async Task Create_DuplicateSessionNumbers_ThrowsValidation()
    {
        var (svc, _) = Build();
        var request = ValidRequest(3);
        request.Sessions[2].SessionNumber = 1; // 1,2,1 — not 1..3 unique

        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(CoachId, request));
    }

    // Extra: two sessions overlapping in time are rejected.
    [Fact]
    public async Task Create_OverlappingSessions_ThrowsValidation()
    {
        var (svc, _) = Build();
        var request = ValidRequest(2);
        // Make session 2 overlap session 1.
        request.Sessions[1].StartTime = request.Sessions[0].StartTime.AddMinutes(30);
        request.Sessions[1].EndTime = request.Sessions[0].EndTime.AddMinutes(30);

        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(CoachId, request));
    }

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeTpRepo : ITrainingPackageRepository
    {
        public TrainingPackage? Added;

        public Task AddAsync(TrainingPackage trainingPackage)
        {
            Added = trainingPackage;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => Task.CompletedTask;

        public Task<TrainingPackage?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<TrainingPackage?> GetByIdWithCoachAsync(Guid id) => throw new NotImplementedException();
        public Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedWithCoachAsync(TrainingPackageFilterRequest filter) => throw new NotImplementedException();
        public Task<TrainingPackage?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<TrainingPackage?> GetOwnedByIdAsync(Guid coachId, Guid id) => throw new NotImplementedException();
        public Task<TrainingPackage?> GetOwnedByIdForUpdateAsync(Guid coachId, Guid id) => throw new NotImplementedException();
        public Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedAsync(TrainingPackageFilterRequest filter) => throw new NotImplementedException();
        public Task<List<TrainingPackageSessionSlot>> GetSessionSlotsForUpdateAsync(Guid packageId) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(TrainingPackage trainingPackage) => throw new NotImplementedException();
    }

    private sealed class FakeCoachRepo : ICoachRepository
    {
        public Task<bool> ExistsByUserIdAsync(Guid userId) => Task.FromResult(true);
        public Task<CoachProfile?> GetByUserIdAsync(Guid userId) => throw new NotImplementedException();
        public Task<CoachProfile?> GetByUserIdWithDetailsAsync(Guid userId) => throw new NotImplementedException();
        public Task<CoachProfile?> GetByUserIdForUpdateAsync(Guid userId) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
        public Task CreateCoachProfileAsync(CoachProfile coachProfile, int coachRoleId, List<int> sportIds) => throw new NotImplementedException();
    }

    private sealed class FakeSportRepo : ISportRepository
    {
        public Task<Sport?> GetByIdAsync(int id)
            => Task.FromResult<Sport?>(new Sport { Id = id, Name = "Tennis", Slug = "tennis", IsActive = true });

        public Task<List<int>> GetActiveSportIdsAsync(List<int> sportIds) => throw new NotImplementedException();
        public Task<bool> ExistsByNameAsync(string name) => throw new NotImplementedException();
        public Task<bool> ExistsBySlugAsync(string slug) => throw new NotImplementedException();
        public Task AddAsync(Sport sport) => throw new NotImplementedException();
    }
}
