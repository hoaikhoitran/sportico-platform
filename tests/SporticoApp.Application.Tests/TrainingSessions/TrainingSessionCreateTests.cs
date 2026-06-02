using Microsoft.Extensions.Logging.Abstractions;
using SporticoApp.Application.DTOs.Availability;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.TrainingSessions;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Services;
using SporticoApp.Application.Tests.Payments; // PassValidator<T>
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.TrainingSessions;

/// <summary>
/// Part C/D: create-session validations and the double-booking guard.
/// The DB-level concurrency guard (filtered unique index) is simulated here by having the
/// repository throw ConflictException on the duplicate insert; the integration index itself
/// is covered by the migration + the live double-book test in the report.
/// </summary>
public class TrainingSessionCreateTests
{
    private static readonly Guid Learner = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Coach = Guid.Parse("c0000000-0000-0000-0000-000000000001");

    private sealed class Ctx
    {
        public TrainingSessionService Service = null!;
        public CoachAvailabilitySlot Slot = null!;
        public Booking Booking = null!;
        public FakeTs Ts = null!;
    }

    private static Ctx Build(
        string bookingStatus = "active",
        DateTime? expiresAt = null,
        int totalSessions = 3,
        int usedSessions = 0,
        string slotStatus = "available",
        Guid? slotCoach = null,
        DateTime? slotStart = null,
        Guid? overlapUserId = null,
        bool addThrowsConflict = false)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            LearnerId = Learner,
            CoachId = Coach,
            Status = bookingStatus,
            TotalSessions = totalSessions,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var slot = new CoachAvailabilitySlot
        {
            Id = Guid.NewGuid(),
            CoachId = slotCoach ?? Coach,
            Status = slotStatus,
            StartTime = slotStart ?? DateTime.UtcNow.AddDays(1),
            EndTime = (slotStart ?? DateTime.UtcNow.AddDays(1)).AddHours(1)
        };
        var ts = new FakeTs { UsedCount = usedSessions, OverlapUserId = overlapUserId, AddThrowsConflict = addThrowsConflict };
        var service = new TrainingSessionService(
            new FakeBookings(booking),
            ts,
            new FakeAvail(slot),
            new FakeWallet(),
            new FakeNotif(),
            NullLogger<TrainingSessionService>.Instance,
            new PassValidator<CreateTrainingSessionRequest>(),
            new PassValidator<ConfirmTrainingSessionRequest>(),
            new PassValidator<CancelTrainingSessionRequest>(),
            new PassValidator<TrainingSessionFilterRequest>());
        return new Ctx { Service = service, Slot = slot, Booking = booking, Ts = ts };
    }

    private static CreateTrainingSessionRequest Req(Ctx c)
        => new() { BookingId = c.Booking.Id, AvailabilitySlotId = c.Slot.Id, LearnerNote = "hi" };

    [Fact]
    public async Task Create_Valid_Succeeds_AndBooksSlot()
    {
        var c = Build();
        var result = await c.Service.CreateAsync(Learner, Req(c));
        Assert.True(result.IsSuccess);
        Assert.Equal(TrainingSessionStatuses.Requested, result.Data!.Status);
        Assert.Equal(CoachAvailabilitySlotStatuses.Booked, c.Slot.Status);
        Assert.NotNull(c.Ts.Added);
    }

    [Fact]
    public async Task Create_SlotAlreadyBooked_Throws409()
    {
        var c = Build(slotStatus: CoachAvailabilitySlotStatuses.Booked);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => c.Service.CreateAsync(Learner, Req(c)));
        Assert.Equal(ErrorCodes.ScheduleConflict, ex.Code);
    }

    [Fact]
    public async Task Create_DoubleBook_UniqueViolation_Throws409()
    {
        // Simulates the concurrent insert losing the race on the filtered unique index.
        var c = Build(addThrowsConflict: true);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => c.Service.CreateAsync(Learner, Req(c)));
        Assert.Equal(ErrorCodes.ScheduleConflict, ex.Code);
    }

    [Fact]
    public async Task Create_BookingExpired_Throws409()
    {
        var c = Build(expiresAt: DateTime.UtcNow.AddDays(-1));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => c.Service.CreateAsync(Learner, Req(c)));
        Assert.Equal(ErrorCodes.BookingNotActive, ex.Code);
    }

    [Fact]
    public async Task Create_BookingNotActive_Throws409()
    {
        var c = Build(bookingStatus: BookingStatuses.PendingPayment);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => c.Service.CreateAsync(Learner, Req(c)));
        Assert.Equal(ErrorCodes.BookingNotActive, ex.Code);
    }

    [Fact]
    public async Task Create_SessionLimitExceeded_Throws409()
    {
        var c = Build(totalSessions: 3, usedSessions: 3);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => c.Service.CreateAsync(Learner, Req(c)));
        Assert.Equal(ErrorCodes.SessionLimitExceeded, ex.Code);
    }

    [Fact]
    public async Task Create_SlotDifferentCoach_Throws403()
    {
        var c = Build(slotCoach: Guid.NewGuid());
        await Assert.ThrowsAsync<ForbiddenException>(() => c.Service.CreateAsync(Learner, Req(c)));
    }

    [Fact]
    public async Task Create_SlotInPast_Throws409()
    {
        var c = Build(slotStart: DateTime.UtcNow.AddHours(-2));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => c.Service.CreateAsync(Learner, Req(c)));
        Assert.Equal(ErrorCodes.ScheduleConflict, ex.Code);
    }

    [Fact]
    public async Task Create_CoachOverlap_Throws409()
    {
        var c = Build(overlapUserId: Coach);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => c.Service.CreateAsync(Learner, Req(c)));
        Assert.Equal(ErrorCodes.ScheduleConflict, ex.Code);
    }

    [Fact]
    public async Task Create_LearnerOverlap_Throws409()
    {
        var c = Build(overlapUserId: Learner);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => c.Service.CreateAsync(Learner, Req(c)));
        Assert.Equal(ErrorCodes.ScheduleConflict, ex.Code);
    }

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeBookings : IBookingRepository
    {
        private readonly Booking _b;
        public FakeBookings(Booking b) => _b = b;
        public Task<Booking?> GetByIdForLearnerForUpdateAsync(Guid learnerId, Guid id)
            => Task.FromResult(_b.Id == id && _b.LearnerId == learnerId ? _b : null);
        public Task<Booking?> GetByIdAsync(Guid id) => Task.FromResult(_b.Id == id ? _b : null);
        public Task<Booking?> GetByIdForUpdateAsync(Guid id) => Task.FromResult(_b.Id == id ? _b : null);
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<Booking?> GetByIdWithTrainingPackageAsync(Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForLearnerAsync(Guid learnerId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForCoachAsync(Guid coachId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForCoachForUpdateAsync(Guid coachId, Guid id) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedByLearnerAsync(Guid learnerId, BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedAsync(BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<Booking?> GetActiveOrCompletedBetweenUsersAsync(Guid learnerId, Guid coachId) => throw new NotImplementedException();
        public Task AddAsync(Booking booking) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(Booking booking) => throw new NotImplementedException();
    }

    private sealed class FakeTs : ITrainingSessionRepository
    {
        public int UsedCount;
        public Guid? OverlapUserId;
        public bool AddThrowsConflict;
        public TrainingSession? Added;

        public Task<int> CountByBookingAsync(Guid bookingId, List<string> statuses) => Task.FromResult(UsedCount);
        public Task<bool> HasOverlapAsync(Guid userId, DateTime s, DateTime e, List<string> st)
            => Task.FromResult(OverlapUserId.HasValue && userId == OverlapUserId.Value);
        public Task AddAsync(TrainingSession session)
        {
            if (AddThrowsConflict)
                throw new ConflictException(ErrorCodes.ScheduleConflict, "Availability slot is no longer available");
            Added = session;
            return Task.CompletedTask;
        }
        public Task<TrainingSession?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<TrainingSession?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<(List<TrainingSession> Items, int TotalCount)> GetByBookingPagedAsync(Guid bookingId, TrainingSessionFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByLearnerAsync(Guid learnerId, TrainingSessionFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, TrainingSessionFilterRequest filter) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(TrainingSession session) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeAvail : ICoachAvailabilityRepository
    {
        private readonly CoachAvailabilitySlot _slot;
        public FakeAvail(CoachAvailabilitySlot slot) => _slot = slot;
        public Task<CoachAvailabilitySlot?> GetByIdForUpdateAsync(Guid id) => Task.FromResult(_slot.Id == id ? _slot : null);
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<CoachAvailabilitySlot?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetByCoachPagedAsync(Guid coachId, CoachAvailabilitySlotFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetAvailableByCoachPagedAsync(Guid coachId, CoachAvailabilitySlotFilterRequest filter) => throw new NotImplementedException();
        public Task<bool> HasOverlapAsync(Guid coachId, DateTime startTime, DateTime endTime, Guid? excludeSlotId = null) => throw new NotImplementedException();
        public Task AddAsync(CoachAvailabilitySlot slot) => throw new NotImplementedException();
    }

    private sealed class FakeWallet : ICoachWalletRepository
    {
        public Task<CoachWallet?> GetByCoachIdForUpdateAsync(Guid coachId) => throw new NotImplementedException();
        public Task<CoachWallet?> GetByCoachIdAsync(Guid coachId) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(CoachWallet wallet) => throw new NotImplementedException();
        public Task AddTransactionWithoutSaveAsync(CoachWalletTransaction transaction) => throw new NotImplementedException();
        public Task AddAsync(CoachWallet wallet) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<(List<CoachWalletTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(Guid coachId, SporticoApp.Application.DTOs.Wallets.CoachWalletTransactionFilterRequest filter) => throw new NotImplementedException();
    }

    private sealed class FakeNotif : INotificationRepository
    {
        public Task<Exception?> TryAddAndSaveAsync(IReadOnlyCollection<Notification> notifications) => Task.FromResult<Exception?>(null);
        public Task AddWithoutSaveAsync(Notification notification) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, SporticoApp.Application.DTOs.Notifications.NotificationFilterRequest filter) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(Guid userId) => throw new NotImplementedException();
        public Task<Notification?> GetByIdForUpdateAsync(Guid userId, Guid notificationId) => throw new NotImplementedException();
        public Task<List<Notification>> GetUnreadForUpdateAsync(Guid userId) => throw new NotImplementedException();
    }
}
