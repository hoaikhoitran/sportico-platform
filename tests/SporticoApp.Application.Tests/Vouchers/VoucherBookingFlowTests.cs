using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Application.Options;
using SporticoApp.Application.Services;
using SporticoApp.Application.Tests.Payments; // shared fakes (FakeBookingRepository, FakeVoucherService, ...)
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Vouchers;

/// <summary>
/// Covers HOW BookingService wires a voucher into the purchase/webhook/reconcile flow: the discount
/// reduces TotalAmount only, never CoachReceiveAmount/PerSessionCoachAmount (both stay computed off
/// OriginalAmount); Payment.Amount matches the discounted total; a 100%-off voucher skips PayOS
/// entirely; and apply/release are called exactly once at the right lifecycle point. Voucher
/// eligibility/quota/budget RULES themselves are covered by VoucherServiceTests — here IVoucherService
/// is a fake so these tests isolate BookingService's own orchestration logic.
/// </summary>
public class VoucherBookingFlowTests
{
    private static readonly Guid LearnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CoachId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static TrainingPackage Package(decimal price, int sessions) => new()
    {
        Id = Guid.NewGuid(),
        CoachId = CoachId,
        DurationDays = 30,
        SessionCount = sessions,
        Price = price,
        Status = TrainingPackageStatuses.Published,
        SessionSlots = BookingPaymentFlowTests.BuildSlots(Guid.NewGuid(), sessions)
    };

    private static BookingService Build(
        TrainingPackage package,
        FakeVoucherService voucherService,
        out FakeBookingRepository bookings,
        out FakePaymentRepository payments,
        out FakeCoachWalletRepository wallets,
        out FakePayOsService payOs,
        Booking? existingBooking = null,
        Payment? existingPayment = null)
    {
        bookings = new FakeBookingRepository(existingBooking);
        payments = new FakePaymentRepository(existingPayment);
        wallets = new FakeCoachWalletRepository(existingBooking != null ? new CoachWallet { Id = Guid.NewGuid(), CoachId = CoachId } : null);
        payOs = new FakePayOsService();

        return new BookingService(
            new FakeTrainingPackageRepository(package),
            bookings,
            new FakeTrainingSessionRepository(),
            payments,
            payOs,
            wallets,
            new FakeNotificationRepository(),
            new FakePlatformSettingRepository(0.15m), // 15% commission to make the OriginalAmount-basis assertion meaningful
            voucherService,
            new FakeBookingSessionUsageService(),
            NullLogger<BookingService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new FeatureOptions { EnableManualPurchase = true }),
            new PassValidator<PurchaseTrainingPackageManualRequest>(),
            new PassValidator<PurchaseTrainingPackagePayOsRequest>(),
            new PassValidator<BookingFilterRequest>());
    }

    private static VoucherReservation Reservation(decimal discount) => new()
    {
        RedemptionId = Guid.NewGuid(),
        VoucherCampaignId = Guid.NewGuid(),
        DiscountAmount = discount,
        CodeSnapshot = "WELCOME10",
        DiscountTypeSnapshot = VoucherDiscountTypes.FixedAmount,
        DiscountValueSnapshot = discount
    };

    // 1. Voucher reduces TotalAmount but CoachReceiveAmount/PerSessionCoachAmount are computed off
    //    the ORIGINAL price — the platform funds the discount, the coach never sees it.
    [Fact]
    public async Task PurchaseManual_WithVoucher_DiscountsTotalAmountOnly_CoachRevenueUnaffected()
    {
        var package = Package(price: 1_000_000m, sessions: 4);
        var voucher = new FakeVoucherService { ReservationToReturn = Reservation(100_000m) };
        var svc = Build(package, voucher, out var bookings, out var payments, out _, out _);

        var result = await svc.PurchaseManualAsync(
            LearnerId, new PurchaseTrainingPackageManualRequest { TrainingPackageId = package.Id, VoucherCode = "WELCOME10" });

        var b = result.Data!;
        Assert.Equal(1_000_000m, b.OriginalAmount);
        Assert.Equal(100_000m, b.DiscountAmount);
        Assert.Equal(900_000m, b.TotalAmount); // learner pays the discounted amount

        // Commission (15%) and coach revenue are computed off the ORIGINAL price, unaffected by the voucher.
        Assert.Equal(150_000m, b.PlatformFeeAmount); // 15% of 1,000,000, NOT of 900,000
        Assert.Equal(850_000m, b.CoachReceiveAmount); // 1,000,000 - 150,000
        Assert.Equal(212_500m, b.PerSessionCoachAmount); // 850,000 / 4

        // Payment amount must equal the discounted total, not the original price.
        var payment = Assert.Single(payments.Added);
        Assert.Equal(900_000m, payment.Amount);

        Assert.Equal(1, voucher.ApplyCallCount); // manual purchase is paid immediately
    }

    [Fact]
    public async Task PurchaseWithPayOs_WithVoucher_PaymentAmountIsDiscountedTotal_NotOriginal()
    {
        var package = Package(price: 1_000_000m, sessions: 4);
        var voucher = new FakeVoucherService { ReservationToReturn = Reservation(300_000m) };
        var svc = Build(package, voucher, out var bookings, out var payments, out _, out var payOs);

        var result = await svc.PurchaseWithPayOsAsync(
            LearnerId, new PurchaseTrainingPackagePayOsRequest { TrainingPackageId = package.Id, VoucherCode = "WELCOME10" });

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.PaymentRequired);
        Assert.NotNull(result.Data.CheckoutUrl);

        var booking = Assert.Single(bookings.Added);
        Assert.Equal(700_000m, booking.TotalAmount);
        Assert.Equal(1_000_000m, booking.OriginalAmount);
        Assert.Equal(300_000m, booking.DiscountAmount);

        var payment = Assert.Single(payments.Added);
        Assert.Equal(700_000m, payment.Amount); // sent-to-PayOS amount is the discounted total
        Assert.Equal(PaymentMethods.PayOs, payment.Method);

        Assert.Equal(0, voucher.ApplyCallCount); // not applied yet — payment is still pending
    }

    // Voucher that covers 100% of the price: no PayOS call, booking active immediately, internal payment.
    [Fact]
    public async Task PurchaseWithPayOs_FullDiscountVoucher_SkipsPayOs_ActivatesImmediately()
    {
        var package = Package(price: 500_000m, sessions: 2);
        var voucher = new FakeVoucherService { ReservationToReturn = Reservation(500_000m) }; // 100% off
        var svc = Build(package, voucher, out var bookings, out var payments, out var wallets, out var payOs);

        var result = await svc.PurchaseWithPayOsAsync(
            LearnerId, new PurchaseTrainingPackagePayOsRequest { TrainingPackageId = package.Id, VoucherCode = "WELCOME10" });

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.PaymentRequired);
        Assert.Null(result.Data.CheckoutUrl);
        Assert.Null(result.Data.OrderCode);
        Assert.Equal(BookingStatuses.Active, result.Data.BookingStatus);
        Assert.Equal(0, payOs.GetStatusCallCount); // CreatePaymentLinkAsync never invoked

        var booking = Assert.Single(bookings.Added);
        Assert.Equal(0m, booking.TotalAmount);
        Assert.Equal(BookingStatuses.Active, booking.Status);

        var payment = Assert.Single(payments.Added);
        Assert.Equal(PaymentMethods.Voucher, payment.Method);
        Assert.Equal(PaymentStatuses.Paid, payment.Status);
        Assert.Equal(0m, payment.Amount);

        Assert.Equal(1, voucher.ApplyCallCount);
        Assert.Equal(1, wallets.WalletCreatedCount);
    }

    // Webhook paid → voucher applied exactly once (idempotent across webhook + reconcile).
    [Fact]
    public async Task Webhook_Paid_AppliesVoucherExactlyOnce_EvenIfCalledTwice()
    {
        var package = Package(price: 1_000_000m, sessions: 4);
        package.SessionSlots = BookingPaymentFlowTests.BuildSlots(package.Id, package.SessionCount);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            LearnerId = LearnerId,
            CoachId = CoachId,
            TrainingPackageId = package.Id,
            TotalAmount = 900_000m,
            OriginalAmount = 1_000_000m,
            DiscountAmount = 100_000m,
            Status = BookingStatuses.PendingPayment,
            TrainingPackage = package,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = LearnerId,
            Amount = 900_000m,
            Method = PaymentMethods.PayOs,
            ReferenceType = PaymentReferenceTypes.Booking,
            ReferenceId = booking.Id,
            Status = PaymentStatuses.Pending,
            OrderCode = 555,
            CreatedAt = DateTime.UtcNow
        };
        var voucher = new FakeVoucherService();
        var svc = Build(package, voucher, out _, out _, out _, out _, booking, payment);

        var webhook = new PayOsWebhookRequest
        {
            Signature = "valid",
            Data = JsonDocument.Parse("{\"orderCode\": 555, \"status\": \"PAID\"}").RootElement
        };

        await svc.HandlePayOsWebhookAsync(webhook);
        await svc.HandlePayOsWebhookAsync(webhook); // simulates webhook + reconcile both firing

        Assert.Equal(BookingStatuses.Active, booking.Status);
        Assert.Equal(1, voucher.ApplyCallCount); // idempotent: booking was already active on the 2nd call
        Assert.Equal(booking.Id, voucher.LastAppliedBookingId);
    }

    // Webhook cancelled → voucher released exactly once with a "payment_cancelled" reason.
    [Fact]
    public async Task Webhook_Cancelled_ReleasesVoucherExactlyOnce()
    {
        var package = Package(price: 1_000_000m, sessions: 4);
        package.SessionSlots = BookingPaymentFlowTests.BuildSlots(package.Id, package.SessionCount);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            LearnerId = LearnerId,
            CoachId = CoachId,
            TrainingPackageId = package.Id,
            TotalAmount = 900_000m,
            OriginalAmount = 1_000_000m,
            DiscountAmount = 100_000m,
            Status = BookingStatuses.PendingPayment,
            TrainingPackage = package,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = LearnerId,
            Amount = 900_000m,
            Method = PaymentMethods.PayOs,
            ReferenceType = PaymentReferenceTypes.Booking,
            ReferenceId = booking.Id,
            Status = PaymentStatuses.Pending,
            OrderCode = 777,
            CreatedAt = DateTime.UtcNow
        };
        var voucher = new FakeVoucherService();
        var svc = Build(package, voucher, out _, out _, out _, out _, booking, payment);

        var webhook = new PayOsWebhookRequest
        {
            Signature = "valid",
            Data = JsonDocument.Parse("{\"orderCode\": 777, \"status\": \"cancelled\"}").RootElement
        };

        await svc.HandlePayOsWebhookAsync(webhook);
        await svc.HandlePayOsWebhookAsync(webhook); // repeated call must not release twice

        Assert.Equal(BookingStatuses.Cancelled, booking.Status);
        Assert.Equal(1, voucher.ReleaseCallCount);
        Assert.Equal("payment_cancelled", voucher.LastReleaseReason);
    }

    // A booking created with NO voucher must behave exactly as before: OriginalAmount == TotalAmount,
    // DiscountAmount == 0, no reserve/apply calls made.
    [Fact]
    public async Task PurchaseManual_NoVoucherCode_BehavesAsBeforeVoucherFeature()
    {
        var package = Package(price: 1_000_000m, sessions: 4);
        var voucher = new FakeVoucherService(); // ReservationToReturn left null
        var svc = Build(package, voucher, out var bookings, out _, out _, out _);

        var result = await svc.PurchaseManualAsync(
            LearnerId, new PurchaseTrainingPackageManualRequest { TrainingPackageId = package.Id });

        var b = result.Data!;
        Assert.Equal(1_000_000m, b.OriginalAmount);
        Assert.Equal(1_000_000m, b.TotalAmount);
        Assert.Equal(0m, b.DiscountAmount);
        Assert.Null(b.VoucherCode);
        Assert.Equal(1, voucher.ApplyCallCount); // Apply is always attempted; it's a no-op with no redemption on record
    }
}
