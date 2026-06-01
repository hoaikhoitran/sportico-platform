using System.Text.Json;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>
/// Covers the payment → booking-activation flow: PayOS purchase, the webhook path,
/// and the new reconcile path — including idempotency between webhook and reconcile.
/// </summary>
public class BookingPaymentFlowTests
{
    private static readonly Guid LearnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CoachId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const long OrderCode = 1234567890;

    private sealed class Scenario
    {
        public BookingService Service = null!;
        public FakeBookingRepository Bookings = null!;
        public FakePaymentRepository Payments = null!;
        public FakeCoachWalletRepository Wallets = null!;
        public FakeNotificationRepository Notifications = null!;
        public FakePayOsService PayOs = null!;
        public Booking Booking = null!;
        public Payment Payment = null!;
    }

    private static Scenario Build(string bookingStatus, string paymentStatus)
    {
        var package = new TrainingPackage
        {
            Id = Guid.NewGuid(),
            CoachId = CoachId,
            DurationDays = 30,
            SessionCount = 10,
            Price = 1000m,
            Status = TrainingPackageStatuses.Published
        };

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            LearnerId = LearnerId,
            CoachId = CoachId,
            TrainingPackageId = package.Id,
            TotalAmount = 1000m,
            Status = bookingStatus,
            TrainingPackage = package,
            PaidAt = bookingStatus == BookingStatuses.Active ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = LearnerId,
            Amount = 1000m,
            Method = PaymentMethods.PayOs,
            ReferenceType = PaymentReferenceTypes.Booking,
            ReferenceId = booking.Id,
            Status = paymentStatus,
            OrderCode = OrderCode,
            PaidAt = paymentStatus == PaymentStatuses.Paid ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        var bookings = new FakeBookingRepository(booking);
        var payments = new FakePaymentRepository(payment);
        var wallets = new FakeCoachWalletRepository(
            bookingStatus == BookingStatuses.Active
                ? new CoachWallet { Id = Guid.NewGuid(), CoachId = CoachId }
                : null);
        var notifications = new FakeNotificationRepository();
        var payos = new FakePayOsService();

        var service = new BookingService(
            new FakeTrainingPackageRepository(package),
            bookings,
            payments,
            payos,
            wallets,
            notifications,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingService>.Instance,
            new PassValidator<PurchaseTrainingPackageManualRequest>(),
            new PassValidator<PurchaseTrainingPackagePayOsRequest>(),
            new PassValidator<BookingFilterRequest>());

        return new Scenario
        {
            Service = service,
            Bookings = bookings,
            Payments = payments,
            Wallets = wallets,
            Notifications = notifications,
            PayOs = payos,
            Booking = booking,
            Payment = payment
        };
    }

    private static PayOsWebhookRequest PaidWebhook(long orderCode) => new()
    {
        Signature = "valid",
        Data = JsonDocument.Parse(
            $"{{\"orderCode\": {orderCode}, \"status\": \"PAID\"}}").RootElement
    };

    // 1. PayOS purchase creates a pending_payment booking and a pending payment.
    [Fact]
    public async Task PurchaseWithPayOs_CreatesPendingBookingAndPendingPayment()
    {
        var package = new TrainingPackage
        {
            Id = Guid.NewGuid(),
            CoachId = CoachId,
            DurationDays = 30,
            SessionCount = 10,
            Price = 1000m,
            Status = TrainingPackageStatuses.Published
        };

        var bookings = new FakeBookingRepository();
        var payments = new FakePaymentRepository();
        var service = new BookingService(
            new FakeTrainingPackageRepository(package),
            bookings,
            payments,
            new FakePayOsService(),
            new FakeCoachWalletRepository(),
            new FakeNotificationRepository(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingService>.Instance,
            new PassValidator<PurchaseTrainingPackageManualRequest>(),
            new PassValidator<PurchaseTrainingPackagePayOsRequest>(),
            new PassValidator<BookingFilterRequest>());

        var result = await service.PurchaseWithPayOsAsync(
            LearnerId,
            new PurchaseTrainingPackagePayOsRequest { TrainingPackageId = package.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatuses.Pending, result.Data!.Status);

        var createdBooking = Assert.Single(bookings.Added);
        Assert.Equal(BookingStatuses.PendingPayment, createdBooking.Status);

        var createdPayment = Assert.Single(payments.Added);
        Assert.Equal(PaymentStatuses.Pending, createdPayment.Status);
        Assert.Null(createdPayment.PaidAt);
    }

    // 2. PayOS paid webhook flips Payment → paid and Booking → active.
    [Fact]
    public async Task Webhook_Paid_ActivatesBookingAndMarksPaymentPaid()
    {
        var s = Build(BookingStatuses.PendingPayment, PaymentStatuses.Pending);

        await s.Service.HandlePayOsWebhookAsync(PaidWebhook(OrderCode));

        Assert.Equal(PaymentStatuses.Paid, s.Payment.Status);
        Assert.NotNull(s.Payment.PaidAt);
        Assert.Equal(BookingStatuses.Active, s.Booking.Status);
        Assert.NotNull(s.Booking.PaidAt);
        Assert.NotNull(s.Booking.ExpiresAt);
        Assert.Equal(1, s.Wallets.WalletCreatedCount);
        Assert.Equal(2, s.Notifications.Count); // coach + learner, once
    }

    // 4 (webhook side). Running the paid webhook twice does not duplicate side effects.
    [Fact]
    public async Task Webhook_Paid_IsIdempotent()
    {
        var s = Build(BookingStatuses.PendingPayment, PaymentStatuses.Pending);

        await s.Service.HandlePayOsWebhookAsync(PaidWebhook(OrderCode));
        await s.Service.HandlePayOsWebhookAsync(PaidWebhook(OrderCode));

        Assert.Equal(BookingStatuses.Active, s.Booking.Status);
        Assert.Equal(1, s.Wallets.WalletCreatedCount); // not created twice
        Assert.Equal(2, s.Notifications.Count);          // not notified twice
    }

    // 3. Reconcile activates the booking when PayOS confirms paid (webhook never ran).
    [Fact]
    public async Task Reconcile_PayOsPaid_ActivatesBooking()
    {
        var s = Build(BookingStatuses.PendingPayment, PaymentStatuses.Pending);
        s.PayOs.StatusResult = new PayOsPaymentStatusResult { Status = "PAID", RawJson = "{}" };

        var result = await s.Service.ReconcilePayOsAsync(
            LearnerId, new ReconcilePayOsRequest { OrderCode = OrderCode });

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.Activated);
        Assert.Equal(PaymentStatuses.Paid, s.Payment.Status);
        Assert.Equal(BookingStatuses.Active, s.Booking.Status);
        Assert.Equal(1, s.Wallets.WalletCreatedCount);
        Assert.Equal(2, s.Notifications.Count);
        Assert.Equal(1, s.PayOs.GetStatusCallCount);
    }

    // 4. Reconcile is idempotent: already-active booking does no PayOS call, no extra effects.
    [Fact]
    public async Task Reconcile_AlreadyActive_IsIdempotent_NoPayOsCall()
    {
        var s = Build(BookingStatuses.Active, PaymentStatuses.Paid);

        var result = await s.Service.ReconcilePayOsAsync(
            LearnerId, new ReconcilePayOsRequest { OrderCode = OrderCode });

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.Activated);
        Assert.Equal(0, s.PayOs.GetStatusCallCount);      // no gateway call
        Assert.Equal(0, s.Wallets.WalletCreatedCount);    // wallet not re-created
        Assert.Equal(0, s.Notifications.Count);           // no duplicate notifications
    }

    // Reconcile + webhook both run: still only one set of side effects.
    [Fact]
    public async Task Reconcile_AfterWebhook_DoesNotDuplicateSideEffects()
    {
        var s = Build(BookingStatuses.PendingPayment, PaymentStatuses.Pending);
        s.PayOs.StatusResult = new PayOsPaymentStatusResult { Status = "PAID", RawJson = "{}" };

        await s.Service.HandlePayOsWebhookAsync(PaidWebhook(OrderCode));
        await s.Service.ReconcilePayOsAsync(LearnerId, new ReconcilePayOsRequest { OrderCode = OrderCode });

        Assert.Equal(BookingStatuses.Active, s.Booking.Status);
        Assert.Equal(1, s.Wallets.WalletCreatedCount);
        Assert.Equal(2, s.Notifications.Count);
        Assert.Equal(0, s.PayOs.GetStatusCallCount); // already settled → no gateway call
    }

    // 3 (negative). PayOS still pending → booking is NOT activated.
    [Fact]
    public async Task Reconcile_PayOsPending_DoesNotActivate()
    {
        var s = Build(BookingStatuses.PendingPayment, PaymentStatuses.Pending);
        s.PayOs.StatusResult = new PayOsPaymentStatusResult { Status = "PENDING", RawJson = "{}" };

        var result = await s.Service.ReconcilePayOsAsync(
            LearnerId, new ReconcilePayOsRequest { OrderCode = OrderCode });

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.Activated);
        Assert.Equal(PaymentStatuses.Pending, s.Payment.Status);
        Assert.Equal(BookingStatuses.PendingPayment, s.Booking.Status);
        Assert.Equal(0, s.Notifications.Count);
    }

    // Ownership guard: a learner cannot reconcile another learner's payment.
    [Fact]
    public async Task Reconcile_ForeignPayment_ThrowsForbidden()
    {
        var s = Build(BookingStatuses.PendingPayment, PaymentStatuses.Pending);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            s.Service.ReconcilePayOsAsync(
                Guid.NewGuid(), new ReconcilePayOsRequest { OrderCode = OrderCode }));

        Assert.Equal(0, s.PayOs.GetStatusCallCount);
    }

    // PayOS cancelled → payment cancelled and pending booking cancelled.
    [Fact]
    public async Task Reconcile_PayOsCancelled_CancelsPaymentAndBooking()
    {
        var s = Build(BookingStatuses.PendingPayment, PaymentStatuses.Pending);
        s.PayOs.StatusResult = new PayOsPaymentStatusResult { Status = "CANCELLED", RawJson = "{}" };

        var result = await s.Service.ReconcilePayOsAsync(
            LearnerId, new ReconcilePayOsRequest { OrderCode = OrderCode });

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.Activated);
        Assert.Equal(PaymentStatuses.Cancelled, s.Payment.Status);
        Assert.Equal(BookingStatuses.Cancelled, s.Booking.Status);
        Assert.NotNull(s.Booking.CancelledAt);
    }

    // Missing payment → NotFound.
    [Fact]
    public async Task Reconcile_UnknownOrderCode_ThrowsNotFound()
    {
        var s = Build(BookingStatuses.PendingPayment, PaymentStatuses.Pending);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            s.Service.ReconcilePayOsAsync(
                LearnerId, new ReconcilePayOsRequest { OrderCode = 99999999 }));
    }
}
