using FluentValidation;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>
/// No-op voucher service for booking-purchase-flow tests that don't exercise vouchers: returns null
/// (no voucher) unless <see cref="ReservationToReturn"/> is set, and just counts Apply/Release calls
/// so tests can assert the booking flow called them at the right point without needing a real
/// VoucherService + EF-backed repositories. Voucher BUSINESS LOGIC itself is covered by
/// SporticoApp.Application.Tests.Vouchers.VoucherServiceTests against the real VoucherService.
/// </summary>
internal sealed class FakeVoucherService : IVoucherService
{
    public VoucherReservation? ReservationToReturn;
    public Exception? ThrowOnReserve;
    public int ApplyCallCount;
    public int ReleaseCallCount;
    public Guid? LastAppliedBookingId;
    public Guid? LastReleasedBookingId;
    public string? LastReleaseReason;

    public Task<Result<VoucherQuoteResponse>> ValidateAsync(Guid learnerId, ValidateVoucherRequest request)
        => throw new NotImplementedException();

    public Task<VoucherReservation?> ReserveForBookingAsync(
        Guid learnerId, string? voucherCode, Guid trainingPackageId, decimal originalAmount, Guid bookingId)
    {
        if (ThrowOnReserve != null) throw ThrowOnReserve;
        if (string.IsNullOrWhiteSpace(voucherCode)) return Task.FromResult<VoucherReservation?>(null);
        return Task.FromResult(ReservationToReturn);
    }

    public Task ApplyForBookingAsync(Guid bookingId, Guid? paymentId)
    {
        ApplyCallCount++;
        LastAppliedBookingId = bookingId;
        return Task.CompletedTask;
    }

    public Task ReleaseForBookingAsync(Guid bookingId, string releaseReason)
    {
        ReleaseCallCount++;
        LastReleasedBookingId = bookingId;
        LastReleaseReason = releaseReason;
        return Task.CompletedTask;
    }

    public Task<Result<VoucherCampaignResponse>> CreateCampaignAsync(Guid adminUserId, CreateVoucherCampaignRequest request) => throw new NotImplementedException();
    public Task<Result<VoucherCampaignResponse>> UpdateCampaignAsync(Guid adminUserId, Guid campaignId, UpdateVoucherCampaignRequest request) => throw new NotImplementedException();
    public Task<Result<VoucherCampaignResponse>> ActivateCampaignAsync(Guid adminUserId, Guid campaignId) => throw new NotImplementedException();
    public Task<Result<VoucherCampaignResponse>> PauseCampaignAsync(Guid adminUserId, Guid campaignId) => throw new NotImplementedException();
    public Task<Result<VoucherCampaignResponse>> EndCampaignAsync(Guid adminUserId, Guid campaignId) => throw new NotImplementedException();
    public Task<Result<PagedResult<VoucherCampaignResponse>>> GetCampaignsAsync(VoucherCampaignFilterRequest filter) => throw new NotImplementedException();
    public Task<Result<VoucherCampaignResponse>> GetCampaignByIdAsync(Guid campaignId) => throw new NotImplementedException();
    public Task<Result<PagedResult<VoucherRedemptionResponse>>> GetRedemptionsAsync(Guid campaignId, VoucherRedemptionFilterRequest filter) => throw new NotImplementedException();
}

/// <summary>
/// In-memory platform settings for booking tests. Defaults to the seeded 0% commission; set
/// <see cref="Setting"/>.CommissionRate (or use the constructor) to simulate an admin-configured rate.
/// </summary>
internal sealed class FakePlatformSettingRepository : IPlatformSettingRepository
{
    public PlatformSetting Setting;
    public int SaveCount;

    public FakePlatformSettingRepository(decimal rate = 0m) => Setting = new PlatformSetting
    {
        Id = PlatformSetting.SingletonId,
        CommissionRate = rate,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public Task<decimal> GetCommissionRateAsync() => Task.FromResult(Setting.CommissionRate);

    public Task<PlatformSetting> GetOrCreateAsync() => Task.FromResult(Setting);

    public Task<PlatformSetting> GetOrCreateForUpdateAsync() => Task.FromResult(Setting);

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>FluentValidation stub that always passes — keeps service tests focused on logic.</summary>
internal sealed class PassValidator<T> : AbstractValidator<T>
{
}

/// <summary>
/// Trivial booking-session-usage stub for tests that don't exercise booking listing/detail
/// (e.g. purchase-flow tests). Reports the booking's total as fully available.
/// </summary>
internal sealed class FakeBookingSessionUsageService
    : SporticoApp.Application.Interfaces.Services.IBookingSessionUsageService
{
    public Task<SporticoApp.Application.DTOs.Bookings.BookingSessionUsage> GetAsync(Guid bookingId, int totalSessions)
        => Task.FromResult(SporticoApp.Application.DTOs.Bookings.BookingSessionUsage.From(
            totalSessions, new Dictionary<string, int>()));

    public Task<IReadOnlyDictionary<Guid, SporticoApp.Application.DTOs.Bookings.BookingSessionUsage>> GetMapAsync(
        IReadOnlyDictionary<Guid, int> bookingTotals)
        => Task.FromResult<IReadOnlyDictionary<Guid, SporticoApp.Application.DTOs.Bookings.BookingSessionUsage>>(
            bookingTotals.ToDictionary(
                kv => kv.Key,
                kv => SporticoApp.Application.DTOs.Bookings.BookingSessionUsage.From(kv.Value, new Dictionary<string, int>())));
}

internal sealed class FakeBookingRepository : IBookingRepository
{
    public Booking? Stored;
    public int SaveCount;
    public readonly List<Booking> Added = new();

    public FakeBookingRepository(Booking? stored = null) => Stored = stored;

    private Booking? Match(Guid id) => Stored != null && Stored.Id == id ? Stored : null;

    public Task<Booking?> GetByIdAsync(Guid id) => Task.FromResult(Match(id));
    public Task<Booking?> GetByIdForUpdateAsync(Guid id) => Task.FromResult(Match(id));
    public Task<Booking?> GetByIdWithTrainingPackageAsync(Guid id) => Task.FromResult(Match(id));

    public Task AddWithoutSaveAsync(Booking booking)
    {
        Added.Add(booking);
        Stored ??= booking;
        return Task.CompletedTask;
    }

    public Task AddAsync(Booking booking)
    {
        Added.Add(booking);
        Stored ??= booking;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<Booking?> GetByIdForLearnerAsync(Guid learnerId, Guid id) => throw new NotImplementedException();
    public Task<Booking?> GetByIdForCoachAsync(Guid coachId, Guid id) => throw new NotImplementedException();
    public Task<Booking?> GetByIdForLearnerForUpdateAsync(Guid learnerId, Guid id) => throw new NotImplementedException();
    public Task<Booking?> GetByIdForCoachForUpdateAsync(Guid coachId, Guid id) => throw new NotImplementedException();
    public Task<(List<Booking> Items, int TotalCount)> GetPagedByLearnerAsync(Guid learnerId, BookingFilterRequest filter) => throw new NotImplementedException();
    public Task<(List<Booking> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, BookingFilterRequest filter) => throw new NotImplementedException();
    public Task<(List<Booking> Items, int TotalCount)> GetPagedAsync(BookingFilterRequest filter) => throw new NotImplementedException();
    public Task<Booking?> GetActiveOrCompletedBetweenUsersAsync(Guid learnerId, Guid coachId) => throw new NotImplementedException();
    public Task<List<Guid>> GetExpiredPendingPaymentBookingIdsAsync(DateTime nowUtc, int batchSize) => Task.FromResult(new List<Guid>());
}

internal sealed class FakePaymentRepository : IPaymentRepository
{
    public Payment? Stored;
    public int TransactionCount;
    public int SaveCount;
    public readonly List<Payment> Added = new();

    public FakePaymentRepository(Payment? stored = null) => Stored = stored;

    public Task<Payment?> GetByIdForUpdateAsync(Guid id)
        => Task.FromResult(Stored != null && Stored.Id == id ? Stored : null);

    public Task<Payment?> GetByOrderCodeForUpdateAsync(long orderCode)
        => Task.FromResult(Stored != null && Stored.OrderCode == orderCode ? Stored : null);

    public Task AddTransactionWithoutSaveAsync(PaymentTransaction transaction)
    {
        TransactionCount++;
        return Task.CompletedTask;
    }

    public Task AddWithoutSaveAsync(Payment payment)
    {
        Added.Add(payment);
        Stored ??= payment;
        return Task.CompletedTask;
    }

    public Task AddAsync(Payment payment)
    {
        Added.Add(payment);
        Stored ??= payment;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<Payment?> GetLatestByReferenceAsync(string referenceType, Guid referenceId) => throw new NotImplementedException();
    public Task<Payment?> GetLatestByReferenceForUpdateAsync(string referenceType, Guid referenceId) => throw new NotImplementedException();
}

internal sealed class FakeCoachWalletRepository : ICoachWalletRepository
{
    public CoachWallet? Wallet;
    public int WalletCreatedCount;

    public FakeCoachWalletRepository(CoachWallet? wallet = null) => Wallet = wallet;

    public Task<CoachWallet?> GetByCoachIdAsync(Guid coachId)
        => Task.FromResult(Wallet != null && Wallet.CoachId == coachId ? Wallet : null);

    public Task AddAsync(CoachWallet wallet)
    {
        Wallet = wallet;
        WalletCreatedCount++;
        return Task.CompletedTask;
    }

    public Task<CoachWallet?> GetByCoachIdForUpdateAsync(Guid coachId)
        => Task.FromResult(Wallet != null && Wallet.CoachId == coachId ? Wallet : null);

    public Task AddWithoutSaveAsync(CoachWallet wallet)
    {
        Wallet = wallet;
        WalletCreatedCount++;
        return Task.CompletedTask;
    }

    public Task<(List<CoachWalletTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(
        Guid coachId, SporticoApp.Application.DTOs.Wallets.CoachWalletTransactionFilterRequest filter)
        => throw new NotImplementedException();

    public Task AddTransactionWithoutSaveAsync(CoachWalletTransaction transaction) => throw new NotImplementedException();
    public Task SaveChangesAsync() => Task.CompletedTask;
}

internal sealed class FakeNotificationRepository : INotificationRepository
{
    public int Count;
    public readonly List<Notification> Notifications = new();

    public Task AddWithoutSaveAsync(Notification notification)
    {
        Count++;
        Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;

    public Task<Exception?> TryAddAndSaveAsync(IReadOnlyCollection<Notification> notifications)
    {
        foreach (var n in notifications)
        {
            Count++;
            Notifications.Add(n);
        }
        return Task.FromResult<Exception?>(null);
    }

    public Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, NotificationFilterRequest filter) => throw new NotImplementedException();
    public Task<int> GetUnreadCountAsync(Guid userId) => throw new NotImplementedException();
    public Task<Notification?> GetByIdForUpdateAsync(Guid userId, Guid notificationId) => throw new NotImplementedException();
    public Task<List<Notification>> GetUnreadForUpdateAsync(Guid userId) => throw new NotImplementedException();
}

internal sealed class FakePayOsService : IPayOsService
{
    public PayOsPaymentStatusResult StatusResult = new();
    public int GetStatusCallCount;
    public bool SignatureValid = true;

    public Task<PayOsPaymentStatusResult> GetPaymentStatusAsync(long orderCode)
    {
        GetStatusCallCount++;
        StatusResult.OrderCode = orderCode;
        return Task.FromResult(StatusResult);
    }

    public Task<CreatePayOsPaymentResult> CreatePaymentLinkAsync(CreatePayOsPaymentRequest request)
        => Task.FromResult(new CreatePayOsPaymentResult
        {
            PaymentLinkId = "link-1",
            CheckoutUrl = "https://pay.example/checkout/1",
            ExpiredAt = DateTime.UtcNow.AddMinutes(15)
        });

    public bool VerifyWebhookSignature(object data, string signature) => SignatureValid;
}

internal sealed class FakeTrainingPackageRepository : ITrainingPackageRepository
{
    public TrainingPackage? Stored;

    public FakeTrainingPackageRepository(TrainingPackage? stored = null) => Stored = stored;

    public Task<TrainingPackage?> GetByIdAsync(Guid id)
        => Task.FromResult(Stored != null && Stored.Id == id ? Stored : null);

    public Task<List<TrainingPackageSessionSlot>> GetSessionSlotsForUpdateAsync(Guid packageId)
        => Task.FromResult(
            Stored != null && Stored.Id == packageId && Stored.SessionSlots != null
                ? Stored.SessionSlots.OrderBy(s => s.SessionNumber).ToList()
                : new List<TrainingPackageSessionSlot>());

    public Task<TrainingPackage?> GetByIdWithCoachAsync(Guid id) => throw new NotImplementedException();
    public Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedWithCoachAsync(TrainingPackageFilterRequest filter) => throw new NotImplementedException();
    public Task<TrainingPackage?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
    public Task<TrainingPackage?> GetOwnedByIdAsync(Guid coachId, Guid id) => throw new NotImplementedException();
    public Task<TrainingPackage?> GetOwnedByIdForUpdateAsync(Guid coachId, Guid id) => throw new NotImplementedException();
    public Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedAsync(TrainingPackageFilterRequest filter) => throw new NotImplementedException();
    public Task AddAsync(TrainingPackage trainingPackage) => throw new NotImplementedException();
    public Task AddWithoutSaveAsync(TrainingPackage trainingPackage) => throw new NotImplementedException();
    public Task SaveChangesAsync() => Task.CompletedTask;
}

/// <summary>
/// Training-session repository stub for booking-purchase flow tests. Captures auto-generated
/// sessions and reports them through the idempotency check so repeated activations are no-ops.
/// </summary>
internal sealed class FakeTrainingSessionRepository : ITrainingSessionRepository
{
    public readonly List<TrainingSession> Added = new();

    public Task<bool> HasPackageGeneratedSessionsAsync(Guid bookingId)
        => Task.FromResult(Added.Any(s => s.BookingId == bookingId && s.TrainingPackageSessionSlotId != null));

    public Task AddRangeWithoutSaveAsync(IEnumerable<TrainingSession> sessions)
    {
        Added.AddRange(sessions);
        return Task.CompletedTask;
    }

    public Task AddWithoutSaveAsync(TrainingSession session)
    {
        Added.Add(session);
        return Task.CompletedTask;
    }

    public Task AddAsync(TrainingSession session)
    {
        Added.Add(session);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;

    public Task<TrainingSession?> GetByIdAsync(Guid id) => throw new NotImplementedException();
    public Task<TrainingSession?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
    public Task<(List<TrainingSession> Items, int TotalCount)> GetByBookingPagedAsync(Guid bookingId, SporticoApp.Application.DTOs.TrainingSessions.TrainingSessionFilterRequest filter) => throw new NotImplementedException();
    public Task<int> CountByBookingAsync(Guid bookingId, List<string> statuses) => throw new NotImplementedException();
    public Task<IReadOnlyDictionary<string, int>> GetStatusCountsByBookingAsync(Guid bookingId) => throw new NotImplementedException();
    public Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, int>>> GetStatusCountsByBookingsAsync(IReadOnlyCollection<Guid> bookingIds) => throw new NotImplementedException();
    public Task<int> CountActiveByAvailabilitySlotIdAsync(Guid slotId, IEnumerable<string> statuses, Guid? excludeSessionId = null) => throw new NotImplementedException();
    public Task<IReadOnlyDictionary<Guid, int>> CountActiveByAvailabilitySlotIdsAsync(IReadOnlyCollection<Guid> slotIds, IEnumerable<string> statuses) => throw new NotImplementedException();
    public Task<bool> HasOverlapAsync(Guid userId, DateTime startTime, DateTime endTime, List<string> activeStatuses) => throw new NotImplementedException();
    public Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByLearnerAsync(Guid learnerId, SporticoApp.Application.DTOs.TrainingSessions.TrainingSessionFilterRequest filter) => throw new NotImplementedException();
    public Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, SporticoApp.Application.DTOs.TrainingSessions.TrainingSessionFilterRequest filter) => throw new NotImplementedException();
}
