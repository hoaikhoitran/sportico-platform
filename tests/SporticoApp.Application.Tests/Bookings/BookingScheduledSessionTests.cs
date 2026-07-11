using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.Options;
using SporticoApp.Application.Services;
using SporticoApp.Application.Tests.Payments; // shared payment-flow fakes + PassValidator + BuildSlots
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Bookings;

/// <summary>
/// The new purchase → auto-schedule flow: manual purchase consumes one seat per package session and
/// auto-creates the training sessions; capacity is enforced; learners are never blocked by their own
/// schedule overlap; PayOS activation is idempotent; PayOS cancellation releases reserved seats.
/// </summary>
public class BookingScheduledSessionTests
{
    private static readonly Guid Learner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Coach = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const long OrderCode = 987654321;

    private static TrainingPackage Package(int sessionCount, int maxParticipants = 5)
    {
        var package = new TrainingPackage
        {
            Id = Guid.NewGuid(),
            CoachId = Coach,
            DurationDays = 30,
            SessionCount = sessionCount,
            Price = 1000m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(40),
            Status = TrainingPackageStatuses.Published
        };
        package.SessionSlots = BookingPaymentFlowTests.BuildSlots(package.Id, sessionCount, maxParticipants);
        return package;
    }

    private sealed class Harness
    {
        public BookingService Svc = null!;
        public FakeBookingRepository Bookings = null!;
        public FakePaymentRepository Payments = null!;
        public FakeTrainingSessionRepository Sessions = null!;
        public FakeCoachWalletRepository Wallets = null!;
        public FakeNotificationRepository Notifications = null!;
        public FakePayOsService PayOs = null!;
    }

    private static Harness Build(
        TrainingPackage package,
        Booking? existingBooking = null,
        Payment? existingPayment = null,
        CoachWallet? wallet = null)
    {
        var bookings = new FakeBookingRepository(existingBooking);
        var payments = new FakePaymentRepository(existingPayment);
        var sessions = new FakeTrainingSessionRepository();
        var wallets = new FakeCoachWalletRepository(wallet);
        var notifications = new FakeNotificationRepository();
        var payos = new FakePayOsService();

        var svc = new BookingService(
            new FakeTrainingPackageRepository(package),
            bookings,
            sessions,
            payments,
            payos,
            wallets,
            notifications,
            new FakePlatformSettingRepository(),
            new FakeBookingSessionUsageService(),
            NullLogger<BookingService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new FeatureOptions { EnableManualPurchase = true }),
            new PassValidator<PurchaseTrainingPackageManualRequest>(),
            new PassValidator<PurchaseTrainingPackagePayOsRequest>(),
            new PassValidator<BookingFilterRequest>());

        return new Harness
        {
            Svc = svc,
            Bookings = bookings,
            Payments = payments,
            Sessions = sessions,
            Wallets = wallets,
            Notifications = notifications,
            PayOs = payos
        };
    }

    private static PayOsWebhookRequest PaidWebhook(long orderCode) => new()
    {
        Signature = "valid",
        Data = JsonDocument.Parse($"{{\"orderCode\": {orderCode}, \"status\": \"PAID\"}}").RootElement
    };

    // 5 + 13/14/15. Manual purchase creates an active booking and auto-creates one scheduled session
    // per package slot, carrying the learner/coach/booking keys the session lists filter on.
    [Fact]
    public async Task ManualPurchase_CreatesActiveBooking_AndAutoCreatesAllSessions()
    {
        var package = Package(3);
        var h = Build(package);

        var result = await h.Svc.PurchaseManualAsync(
            Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = package.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatuses.Active, result.Data!.Status);

        var bookingId = result.Data.Id;
        Assert.Equal(3, h.Sessions.Added.Count);
        Assert.All(h.Sessions.Added, s =>
        {
            Assert.Equal(bookingId, s.BookingId);          // booking detail session list
            Assert.Equal(Learner, s.LearnerId);            // learner session list
            Assert.Equal(Coach, s.CoachId);                // coach session list
            Assert.Equal(TrainingSessionStatuses.Scheduled, s.Status);
            Assert.NotNull(s.TrainingPackageSessionSlotId);
        });
    }

    // 6. Manual purchase increments booked participants for every package session slot.
    [Fact]
    public async Task ManualPurchase_IncrementsBookedParticipants_ForEverySlot()
    {
        var package = Package(3, maxParticipants: 5);
        var h = Build(package);

        await h.Svc.PurchaseManualAsync(
            Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = package.Id });

        Assert.All(package.SessionSlots, s =>
        {
            Assert.Equal(1, s.BookedParticipants);
            Assert.Equal(TrainingPackageSessionSlotStatuses.Open, s.Status); // 1 of 5 → still open
        });
    }

    // 6b. Taking the last seat flips the slot to full.
    [Fact]
    public async Task ManualPurchase_LastSeat_MarksSlotFull()
    {
        var package = Package(2, maxParticipants: 1);
        var h = Build(package);

        await h.Svc.PurchaseManualAsync(
            Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = package.Id });

        Assert.All(package.SessionSlots, s =>
        {
            Assert.Equal(1, s.BookedParticipants);
            Assert.Equal(TrainingPackageSessionSlotStatuses.Full, s.Status);
        });
    }

    // 7. Purchase is rejected (and nothing is created) if any package session slot is full.
    [Fact]
    public async Task ManualPurchase_AnySlotFull_ThrowsConflict_AndCreatesNothing()
    {
        var package = Package(3, maxParticipants: 2);
        package.SessionSlots.ElementAt(1).BookedParticipants = 2; // full
        package.SessionSlots.ElementAt(1).Status = TrainingPackageSessionSlotStatuses.Full;
        var h = Build(package);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Svc.PurchaseManualAsync(
                Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = package.Id }));

        Assert.Equal(ErrorCodes.TrainingPackageSessionSlotFull, ex.Code);
        Assert.Empty(h.Bookings.Added);
        Assert.Empty(h.Sessions.Added);
        // The other slots were not reserved (capacity is checked before any seat is consumed).
        Assert.All(package.SessionSlots, s => Assert.True(s.BookedParticipants <= 2));
        Assert.Equal(0, package.SessionSlots.ElementAt(0).BookedParticipants);
    }

    // 7b. A package with no schedule cannot be purchased (clear conflict, not a 500).
    [Fact]
    public async Task ManualPurchase_NoSchedule_ThrowsConflict()
    {
        var package = Package(0);
        package.SessionSlots.Clear();
        var h = Build(package);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Svc.PurchaseManualAsync(
                Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = package.Id }));

        Assert.Equal(ErrorCodes.TrainingPackageHasNoSchedule, ex.Code);
    }

    // 8. A learner can buy packages whose sessions overlap (e.g. buying for a child): the new flow
    // never applies a learner schedule-overlap rule — proven by HasOverlapAsync never being called
    // (the fake throws if it is) and both overlapping purchases succeeding.
    [Fact]
    public async Task ManualPurchase_OverlappingPackages_BothSucceed_NoLearnerOverlapCheck()
    {
        var packageA = Package(2);
        var packageB = Package(2);
        // Force B's schedule to overlap A's exactly.
        for (var i = 0; i < packageB.SessionSlots.Count; i++)
        {
            packageB.SessionSlots.ElementAt(i).StartTime = packageA.SessionSlots.ElementAt(i).StartTime;
            packageB.SessionSlots.ElementAt(i).EndTime = packageA.SessionSlots.ElementAt(i).EndTime;
        }

        var hA = Build(packageA);
        var hB = Build(packageB);

        var resultA = await hA.Svc.PurchaseManualAsync(
            Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageA.Id });
        var resultB = await hB.Svc.PurchaseManualAsync(
            Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageB.Id });

        Assert.True(resultA.IsSuccess);
        Assert.True(resultB.IsSuccess);
        Assert.Equal(2, hA.Sessions.Added.Count);
        Assert.Equal(2, hB.Sessions.Added.Count);
    }

    // 9. PayOS activation creates the sessions only once even if the webhook fires repeatedly.
    [Fact]
    public async Task PayOsActivation_CreatesSessionsOnce_AcrossRepeatedWebhooks()
    {
        var package = Package(3);
        foreach (var slot in package.SessionSlots) // already reserved at pending-payment time
        {
            slot.BookedParticipants = 1;
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            LearnerId = Learner,
            CoachId = Coach,
            TrainingPackageId = package.Id,
            TotalAmount = 1000m,
            TotalSessions = 3,
            Status = BookingStatuses.PendingPayment,
            TrainingPackage = package,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Learner,
            Amount = 1000m,
            Method = PaymentMethods.PayOs,
            ReferenceType = PaymentReferenceTypes.Booking,
            ReferenceId = booking.Id,
            Status = PaymentStatuses.Pending,
            OrderCode = OrderCode,
            CreatedAt = DateTime.UtcNow
        };

        var h = Build(package, booking, payment);

        await h.Svc.HandlePayOsWebhookAsync(PaidWebhook(OrderCode));
        await h.Svc.HandlePayOsWebhookAsync(PaidWebhook(OrderCode));

        Assert.Equal(BookingStatuses.Active, booking.Status);
        Assert.Equal(3, h.Sessions.Added.Count); // generated exactly once
    }

    // 10. PayOS cancellation releases the reserved seats and cancels the booking.
    [Fact]
    public async Task PayOsCancellation_ReleasesReservedSlots()
    {
        var package = Package(3, maxParticipants: 1);
        foreach (var slot in package.SessionSlots) // reserved → full at pending time
        {
            slot.BookedParticipants = 1;
            slot.Status = TrainingPackageSessionSlotStatuses.Full;
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            LearnerId = Learner,
            CoachId = Coach,
            TrainingPackageId = package.Id,
            TotalAmount = 1000m,
            TotalSessions = 3,
            Status = BookingStatuses.PendingPayment,
            TrainingPackage = package,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Learner,
            Amount = 1000m,
            Method = PaymentMethods.PayOs,
            ReferenceType = PaymentReferenceTypes.Booking,
            ReferenceId = booking.Id,
            Status = PaymentStatuses.Pending,
            OrderCode = OrderCode,
            CreatedAt = DateTime.UtcNow
        };

        var h = Build(package, booking, payment);
        h.PayOs.StatusResult = new PayOsPaymentStatusResult { Status = "CANCELLED", RawJson = "{}" };

        var result = await h.Svc.ReconcilePayOsAsync(
            Learner, new ReconcilePayOsRequest { OrderCode = OrderCode });

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatuses.Cancelled, booking.Status);
        Assert.All(package.SessionSlots, s =>
        {
            Assert.Equal(0, s.BookedParticipants);                       // seat released
            Assert.Equal(TrainingPackageSessionSlotStatuses.Open, s.Status);
        });
    }
}
