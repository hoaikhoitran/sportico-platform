using SporticoApp.Application.DTOs.Availability;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Services;
using SporticoApp.Application.Tests.Payments; // PassValidator<T>
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Availability;

/// <summary>Part B: group-slot capacity — creation defaults, capacity fields, cancel guard.</summary>
public class CoachAvailabilityServiceTests
{
    private static readonly Guid Coach = Guid.Parse("c0000000-0000-0000-0000-0000000000aa");

    private static CoachAvailabilityService Build(FakeAvail avail, FakeTs ts)
        => new(
            avail,
            new FakeCoach(),
            ts,
            new PassValidator<CreateCoachAvailabilitySlotRequest>(),
            new PassValidator<CoachAvailabilitySlotFilterRequest>());

    private static CreateCoachAvailabilitySlotRequest CreateReq(int? maxParticipants)
        => new()
        {
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            IsOnline = true,
            MaxParticipants = maxParticipants
        };

    [Fact]
    public async Task CreateSlot_WithoutMaxParticipants_DefaultsToOne()
    {
        var avail = new FakeAvail();
        var service = Build(avail, new FakeTs());

        var result = await service.CreateSlotAsync(Coach, CreateReq(maxParticipants: null));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.MaxParticipants);
        Assert.Equal(0, result.Data.BookedParticipants);
        Assert.Equal(1, result.Data.RemainingParticipants);
        Assert.False(result.Data.IsFull);
        Assert.Equal(1, avail.Added!.MaxParticipants);
    }

    [Fact]
    public async Task CreateSlot_WithMaxParticipants5_ReturnsCapacityFields()
    {
        var avail = new FakeAvail();
        var service = Build(avail, new FakeTs());

        var result = await service.CreateSlotAsync(Coach, CreateReq(maxParticipants: 5));

        Assert.Equal(5, result.Data!.MaxParticipants);
        Assert.Equal(0, result.Data.BookedParticipants);
        Assert.Equal(5, result.Data.RemainingParticipants);
        Assert.False(result.Data.IsFull);
    }

    [Fact]
    public async Task GetMySlots_PopulatesCapacityFromActiveCount()
    {
        var slot = Slot(maxParticipants: 5, status: CoachAvailabilitySlotStatuses.Available);
        var avail = new FakeAvail { CoachPage = new List<CoachAvailabilitySlot> { slot } };
        var ts = new FakeTs { Counts = new Dictionary<Guid, int> { [slot.Id] = 2 } };
        var service = Build(avail, ts);

        var result = await service.GetMySlotsAsync(Coach, new CoachAvailabilitySlotFilterRequest());

        var dto = Assert.Single(result.Data!.Items);
        Assert.Equal(5, dto.MaxParticipants);
        Assert.Equal(2, dto.BookedParticipants);
        Assert.Equal(3, dto.RemainingParticipants);
        Assert.False(dto.IsFull);
    }

    [Fact]
    public async Task GetPublicSlots_OnlyReturnsRepositoryBookableSlots_WithCapacity()
    {
        // The repo (faked) already returns only available+future slots; service maps capacity.
        var slot = Slot(maxParticipants: 2, status: CoachAvailabilitySlotStatuses.Available);
        var avail = new FakeAvail { AvailablePage = new List<CoachAvailabilitySlot> { slot } };
        var ts = new FakeTs { Counts = new Dictionary<Guid, int> { [slot.Id] = 1 } };
        var service = Build(avail, ts);

        var result = await service.GetCoachPublicSlotsAsync(Coach, new CoachAvailabilitySlotFilterRequest());

        var dto = Assert.Single(result.Data!.Items);
        Assert.Equal(1, dto.RemainingParticipants); // 2 - 1
        Assert.False(dto.IsFull);
    }

    [Fact]
    public async Task CancelSlot_WithActiveSessions_Throws409()
    {
        var slot = Slot(maxParticipants: 3, status: CoachAvailabilitySlotStatuses.Available);
        var avail = new FakeAvail { ForUpdate = slot };
        var ts = new FakeTs { SingleCount = 2 }; // 2 active sessions
        var service = Build(avail, ts);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => service.CancelSlotAsync(Coach, slot.Id));
        Assert.Equal(ErrorCodes.InvalidTrainingSessionStatus, ex.Code);
        Assert.NotEqual(CoachAvailabilitySlotStatuses.Cancelled, slot.Status);
    }

    [Fact]
    public async Task CancelSlot_NoActiveSessions_Cancels()
    {
        var slot = Slot(maxParticipants: 3, status: CoachAvailabilitySlotStatuses.Available);
        var avail = new FakeAvail { ForUpdate = slot };
        var ts = new FakeTs { SingleCount = 0 };
        var service = Build(avail, ts);

        var result = await service.CancelSlotAsync(Coach, slot.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(CoachAvailabilitySlotStatuses.Cancelled, slot.Status);
    }

    private static CoachAvailabilitySlot Slot(int maxParticipants, string status) => new()
    {
        Id = Guid.NewGuid(),
        CoachId = Coach,
        Status = status,
        MaxParticipants = maxParticipants,
        StartTime = DateTime.UtcNow.AddDays(1),
        EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeAvail : ICoachAvailabilityRepository
    {
        public CoachAvailabilitySlot? Added;
        public CoachAvailabilitySlot? ForUpdate;
        public List<CoachAvailabilitySlot> CoachPage = new();
        public List<CoachAvailabilitySlot> AvailablePage = new();

        public Task AddAsync(CoachAvailabilitySlot slot) { Added = slot; return Task.CompletedTask; }
        public Task<bool> HasOverlapAsync(Guid coachId, DateTime startTime, DateTime endTime, Guid? excludeSlotId = null) => Task.FromResult(false);
        public Task<CoachAvailabilitySlot?> GetByIdForUpdateAsync(Guid id) => Task.FromResult(ForUpdate);
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetByCoachPagedAsync(Guid coachId, CoachAvailabilitySlotFilterRequest filter)
            => Task.FromResult((CoachPage, CoachPage.Count));
        public Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetAvailableByCoachPagedAsync(Guid coachId, CoachAvailabilitySlotFilterRequest filter)
            => Task.FromResult((AvailablePage, AvailablePage.Count));
        public Task<CoachAvailabilitySlot?> GetByIdAsync(Guid id) => throw new NotImplementedException();
    }

    private sealed class FakeTs : ITrainingSessionRepository
    {
        public int SingleCount;
        public Dictionary<Guid, int> Counts = new();

        public Task<int> CountActiveByAvailabilitySlotIdAsync(Guid slotId, IEnumerable<string> statuses, Guid? excludeSessionId = null)
            => Task.FromResult(SingleCount);
        public Task<IReadOnlyDictionary<Guid, int>> CountActiveByAvailabilitySlotIdsAsync(IReadOnlyCollection<Guid> slotIds, IEnumerable<string> statuses)
            => Task.FromResult<IReadOnlyDictionary<Guid, int>>(Counts);

        public Task<int> CountByBookingAsync(Guid bookingId, List<string> statuses) => throw new NotImplementedException();
        public Task<bool> HasOverlapAsync(Guid userId, DateTime s, DateTime e, List<string> st) => throw new NotImplementedException();
        public Task<TrainingSession?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<TrainingSession?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<(List<TrainingSession> Items, int TotalCount)> GetByBookingPagedAsync(Guid bookingId, SporticoApp.Application.DTOs.TrainingSessions.TrainingSessionFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByLearnerAsync(Guid learnerId, SporticoApp.Application.DTOs.TrainingSessions.TrainingSessionFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, SporticoApp.Application.DTOs.TrainingSessions.TrainingSessionFilterRequest filter) => throw new NotImplementedException();
        public Task AddAsync(TrainingSession session) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(TrainingSession session) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    private sealed class FakeCoach : ICoachRepository
    {
        public Task<bool> ExistsByUserIdAsync(Guid userId) => Task.FromResult(true);
        public Task<CoachProfile?> GetByUserIdAsync(Guid userId) => throw new NotImplementedException();
        public Task<CoachProfile?> GetByUserIdWithDetailsAsync(Guid userId) => throw new NotImplementedException();
        public Task<CoachProfile?> GetByUserIdForUpdateAsync(Guid userId) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task CreateCoachProfileAsync(CoachProfile coachProfile, int coachRoleId, List<int> sportIds) => throw new NotImplementedException();
    }
}
