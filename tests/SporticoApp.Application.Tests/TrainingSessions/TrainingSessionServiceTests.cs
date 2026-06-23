using Microsoft.Extensions.Logging.Abstractions;
using SporticoApp.Application.DTOs.Availability;
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
/// Regression tests for the P0 fix: a notification side-effect failure (e.g. an unknown
/// notification type rejected by the DB CHECK constraint) must NOT turn a successful
/// session mutation into HTTP 500. Plus cancel slot-release atomicity and complete
/// double-credit safety.
/// </summary>
public class TrainingSessionServiceTests
{
    private static readonly Guid CoachId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid LearnerId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static TrainingSessionService Build(
        FakeTsRepo ts,
        FakeNotifRepo notif,
        FakeAvailRepo? avail = null,
        FakeBookingRepo? bookings = null,
        FakeWalletRepo? wallet = null)
        => new(
            bookings ?? new FakeBookingRepo(),
            ts,
            avail ?? new FakeAvailRepo(),
            wallet ?? new FakeWalletRepo(),
            notif,
            new BookingSessionUsageService(ts),
            NullLogger<TrainingSessionService>.Instance,
            new PassValidator<CreateTrainingSessionRequest>(),
            new PassValidator<ConfirmTrainingSessionRequest>(),
            new PassValidator<CancelTrainingSessionRequest>(),
            new PassValidator<TrainingSessionFilterRequest>());

    private static TrainingSession Session(string status) => new()
    {
        Id = Guid.NewGuid(),
        BookingId = Guid.NewGuid(),
        CoachId = CoachId,
        LearnerId = LearnerId,
        Status = status,
        StartTime = DateTime.UtcNow.AddDays(1),
        EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // P0: confirm succeeds even when the notification insert fails.
    [Fact]
    public async Task Confirm_NotificationFails_StillReturnsSuccess()
    {
        var session = Session(TrainingSessionStatuses.Requested);
        var ts = new FakeTsRepo { Session = session };
        var notif = new FakeNotifRepo { ToReturn = new InvalidOperationException("CHECK violation: type") };
        var service = Build(ts, notif);

        var result = await service.ConfirmAsync(CoachId, session.Id, new ConfirmTrainingSessionRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(TrainingSessionStatuses.Scheduled, session.Status);
        Assert.True(ts.SaveCount >= 1); // main mutation committed
    }

    // Happy path: notification created with the correct type.
    [Fact]
    public async Task Confirm_HappyPath_CreatesTrainingSessionNotification()
    {
        var session = Session(TrainingSessionStatuses.Requested);
        var ts = new FakeTsRepo { Session = session };
        var notif = new FakeNotifRepo { ToReturn = null };
        var service = Build(ts, notif);

        var result = await service.ConfirmAsync(CoachId, session.Id, new ConfirmTrainingSessionRequest());

        Assert.True(result.IsSuccess);
        var n = Assert.Single(notif.Saved);
        Assert.Equal(NotificationTypeConstants.TrainingSession, n.Type);
        Assert.Equal(session.LearnerId, n.UserId);
    }

    // Cancel: slot released in the same unit of work, and notification failure does not 500.
    [Fact]
    public async Task Cancel_ReleasesSlot_AndNotificationFailureStillSucceeds()
    {
        var session = Session(TrainingSessionStatuses.Scheduled);
        var slot = new CoachAvailabilitySlot
        {
            Id = Guid.NewGuid(),
            CoachId = CoachId,
            Status = CoachAvailabilitySlotStatuses.Booked,
            StartTime = session.StartTime,
            EndTime = session.EndTime
        };
        session.AvailabilitySlotId = slot.Id;

        var ts = new FakeTsRepo { Session = session };
        var avail = new FakeAvailRepo { Slot = slot };
        var notif = new FakeNotifRepo { ToReturn = new InvalidOperationException("CHECK violation") };
        var service = Build(ts, notif, avail);

        var result = await service.CancelAsync(CoachId, session.Id, new CancelTrainingSessionRequest { Reason = "busy" });

        Assert.True(result.IsSuccess);
        Assert.Equal(TrainingSessionStatuses.Cancelled, session.Status);
        Assert.Equal(CoachAvailabilitySlotStatuses.Available, slot.Status); // slot released
    }

    // Cancel on a full group slot frees one seat → slot re-opens to available.
    [Fact]
    public async Task Cancel_GroupSlot_ReleasesSeat_ReopensSlot()
    {
        var session = Session(TrainingSessionStatuses.Scheduled);
        var slot = new CoachAvailabilitySlot
        {
            Id = Guid.NewGuid(),
            CoachId = CoachId,
            Status = CoachAvailabilitySlotStatuses.Booked, // was full (3/3)
            MaxParticipants = 3,
            StartTime = DateTime.UtcNow.AddDays(1),        // future
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        };
        session.StartTime = slot.StartTime;
        session.EndTime = slot.EndTime;
        session.AvailabilitySlotId = slot.Id;

        // Two OTHER active sessions remain after this one is cancelled (2 < 3 → reopen).
        var ts = new FakeTsRepo { Session = session, ActiveSlotCount = 2 };
        var avail = new FakeAvailRepo { Slot = slot };
        var service = Build(ts, new FakeNotifRepo(), avail);

        var result = await service.CancelAsync(CoachId, session.Id, new CancelTrainingSessionRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(CoachAvailabilitySlotStatuses.Available, slot.Status);
    }

    // Complete: second call returns 409 and does NOT credit the wallet again.
    [Fact]
    public async Task Complete_SecondCall_ReturnsConflict_NoDoubleCredit()
    {
        var session = Session(TrainingSessionStatuses.Scheduled);
        var booking = new Booking
        {
            Id = session.BookingId,
            CoachId = CoachId,
            LearnerId = LearnerId,
            TotalSessions = 3,
            CompletedSessions = 0,
            PerSessionCoachAmount = 100m,
            Status = BookingStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var walletEntity = new CoachWallet { Id = Guid.NewGuid(), CoachId = CoachId };

        var ts = new FakeTsRepo { Session = session };
        var bookings = new FakeBookingRepo { Booking = booking };
        var wallet = new FakeWalletRepo { Wallet = walletEntity };
        var service = Build(ts, new FakeNotifRepo(), bookings: bookings, wallet: wallet);

        var first = await service.CompleteAsync(CoachId, session.Id);
        Assert.True(first.IsSuccess);
        Assert.Equal(100m, walletEntity.AvailableBalance);
        Assert.Equal(1, booking.CompletedSessions);
        Assert.Single(wallet.Transactions);

        await Assert.ThrowsAsync<ConflictException>(() => service.CompleteAsync(CoachId, session.Id));

        // No second credit, no second ledger entry, no extra increment.
        Assert.Equal(100m, walletEntity.AvailableBalance);
        Assert.Equal(1, booking.CompletedSessions);
        Assert.Single(wallet.Transactions);
    }

    // Part E: completing a non-scheduled session is a 409 (requested / cancelled / completed).
    [Theory]
    [InlineData(TrainingSessionStatuses.Requested)]
    [InlineData(TrainingSessionStatuses.Cancelled)]
    [InlineData(TrainingSessionStatuses.Completed)]
    public async Task Complete_NonScheduledSession_ThrowsConflict(string status)
    {
        var session = Session(status);
        var ts = new FakeTsRepo { Session = session };
        var service = Build(ts, new FakeNotifRepo(), bookings: new FakeBookingRepo { Booking = null });

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CompleteAsync(CoachId, session.Id));
        Assert.Equal(ErrorCodes.InvalidTrainingSessionStatus, ex.Code);
    }

    // Part E: booking becomes 'completed' only once CompletedSessions >= TotalSessions.
    [Fact]
    public async Task Complete_LastSession_MarksBookingCompleted()
    {
        var session = Session(TrainingSessionStatuses.Scheduled);
        var booking = new Booking
        {
            Id = session.BookingId,
            CoachId = CoachId,
            LearnerId = LearnerId,
            TotalSessions = 1,
            CompletedSessions = 0,
            PerSessionCoachAmount = 100m,
            Status = BookingStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var ts = new FakeTsRepo { Session = session };
        var bookings = new FakeBookingRepo { Booking = booking };
        var wallet = new FakeWalletRepo { Wallet = new CoachWallet { Id = Guid.NewGuid(), CoachId = CoachId } };
        var service = Build(ts, new FakeNotifRepo(), bookings: bookings, wallet: wallet);

        var result = await service.CompleteAsync(CoachId, session.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatuses.Completed, booking.Status);
        Assert.NotNull(booking.CompletedAt);
    }

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeTsRepo : ITrainingSessionRepository
    {
        public TrainingSession? Session;
        public int SaveCount;
        public int ActiveSlotCount; // OTHER active sessions on the slot (capacity)

        public Task<TrainingSession?> GetByIdForUpdateAsync(Guid id)
            => Task.FromResult(Session != null && Session.Id == id ? Session : null);
        public Task SaveChangesAsync() { SaveCount++; return Task.CompletedTask; }
        public Task AddAsync(TrainingSession session) { Session ??= session; SaveCount++; return Task.CompletedTask; }
        public Task<int> CountByBookingAsync(Guid bookingId, List<string> statuses) => Task.FromResult(CompletedInDbCount);
        public int CompletedInDbCount; // committed completed sessions (for CompleteAsync self-heal)
        public Task<IReadOnlyDictionary<string, int>> GetStatusCountsByBookingAsync(Guid bookingId)
            => Task.FromResult<IReadOnlyDictionary<string, int>>(new Dictionary<string, int>());
        public Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, int>>> GetStatusCountsByBookingsAsync(IReadOnlyCollection<Guid> bookingIds)
            => Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, int>>>(new Dictionary<Guid, IReadOnlyDictionary<string, int>>());
        public Task<int> CountActiveByAvailabilitySlotIdAsync(Guid slotId, IEnumerable<string> statuses, Guid? excludeSessionId = null)
            => Task.FromResult(ActiveSlotCount);
        public Task<IReadOnlyDictionary<Guid, int>> CountActiveByAvailabilitySlotIdsAsync(IReadOnlyCollection<Guid> slotIds, IEnumerable<string> statuses)
            => Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());
        public Task<bool> HasOverlapAsync(Guid userId, DateTime s, DateTime e, List<string> st) => Task.FromResult(false);
        public Task<bool> HasPackageGeneratedSessionsAsync(Guid bookingId) => Task.FromResult(false);
        public Task AddRangeWithoutSaveAsync(IEnumerable<TrainingSession> sessions) => Task.CompletedTask;

        public Task<TrainingSession?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<(List<TrainingSession> Items, int TotalCount)> GetByBookingPagedAsync(Guid bookingId, TrainingSessionFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByLearnerAsync(Guid learnerId, TrainingSessionFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, TrainingSessionFilterRequest filter) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(TrainingSession session) => throw new NotImplementedException();
    }

    private sealed class FakeNotifRepo : INotificationRepository
    {
        public Exception? ToReturn;
        public readonly List<Notification> Saved = new();

        public Task<Exception?> TryAddAndSaveAsync(IReadOnlyCollection<Notification> notifications)
        {
            if (ToReturn is null) Saved.AddRange(notifications);
            return Task.FromResult(ToReturn);
        }

        public Task AddWithoutSaveAsync(Notification notification) { Saved.Add(notification); return Task.CompletedTask; }
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, SporticoApp.Application.DTOs.Notifications.NotificationFilterRequest filter) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(Guid userId) => throw new NotImplementedException();
        public Task<Notification?> GetByIdForUpdateAsync(Guid userId, Guid notificationId) => throw new NotImplementedException();
        public Task<List<Notification>> GetUnreadForUpdateAsync(Guid userId) => throw new NotImplementedException();
    }

    private sealed class FakeAvailRepo : ICoachAvailabilityRepository
    {
        public CoachAvailabilitySlot? Slot;

        public Task<CoachAvailabilitySlot?> GetByIdForUpdateAsync(Guid id)
            => Task.FromResult(Slot != null && Slot.Id == id ? Slot : null);
        public Task SaveChangesAsync() => Task.CompletedTask;

        public Task<CoachAvailabilitySlot?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetByCoachPagedAsync(Guid coachId, CoachAvailabilitySlotFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetAvailableByCoachPagedAsync(Guid coachId, CoachAvailabilitySlotFilterRequest filter) => throw new NotImplementedException();
        public Task<bool> HasOverlapAsync(Guid coachId, DateTime startTime, DateTime endTime, Guid? excludeSlotId = null) => throw new NotImplementedException();
        public Task AddAsync(CoachAvailabilitySlot slot) => throw new NotImplementedException();
    }

    private sealed class FakeBookingRepo : IBookingRepository
    {
        public Booking? Booking;

        public Task<Booking?> GetByIdForUpdateAsync(Guid id)
            => Task.FromResult(Booking != null && Booking.Id == id ? Booking : null);
        public Task SaveChangesAsync() => Task.CompletedTask;

        public Task<Booking?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdWithTrainingPackageAsync(Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForLearnerAsync(Guid learnerId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForCoachAsync(Guid coachId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForLearnerForUpdateAsync(Guid learnerId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForCoachForUpdateAsync(Guid coachId, Guid id) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedByLearnerAsync(Guid learnerId, SporticoApp.Application.DTOs.Bookings.BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, SporticoApp.Application.DTOs.Bookings.BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedAsync(SporticoApp.Application.DTOs.Bookings.BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<Booking?> GetActiveOrCompletedBetweenUsersAsync(Guid learnerId, Guid coachId) => throw new NotImplementedException();
        public Task AddAsync(Booking booking) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(Booking booking) => throw new NotImplementedException();
    }

    private sealed class FakeWalletRepo : ICoachWalletRepository
    {
        public CoachWallet? Wallet;
        public readonly List<CoachWalletTransaction> Transactions = new();

        public Task<CoachWallet?> GetByCoachIdForUpdateAsync(Guid coachId) => Task.FromResult(Wallet);
        public Task<CoachWallet?> GetByCoachIdAsync(Guid coachId) => Task.FromResult(Wallet);
        public Task AddWithoutSaveAsync(CoachWallet wallet) { Wallet = wallet; return Task.CompletedTask; }
        public Task AddTransactionWithoutSaveAsync(CoachWalletTransaction transaction) { Transactions.Add(transaction); return Task.CompletedTask; }
        public Task AddAsync(CoachWallet wallet) { Wallet = wallet; return Task.CompletedTask; }
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<(List<CoachWalletTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(Guid coachId, SporticoApp.Application.DTOs.Wallets.CoachWalletTransactionFilterRequest filter) => throw new NotImplementedException();
    }
}
