using Microsoft.Extensions.Logging.Abstractions;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.Options;
using SporticoApp.Application.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>
/// Part F: the platform commission is read from the persisted platform setting exactly once, when a
/// new booking snapshot is created. The default is 0%; an admin-configured rate applies only to
/// bookings created after the change. Amounts are stored at full decimal precision (no premature
/// rounding); per-session is coachReceive / totalSessions.
/// </summary>
public class PlatformFeeMathTests
{
    private static readonly Guid Learner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Coach = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static (BookingService Svc, FakePlatformSettingRepository Settings, FakeBookingRepository Bookings, Guid PackageId)
        Build(decimal price, int sessions, decimal? rate = null)
    {
        var package = new TrainingPackage
        {
            Id = Guid.NewGuid(),
            CoachId = Coach,
            DurationDays = 30,
            SessionCount = sessions,
            Price = price,
            Status = TrainingPackageStatuses.Published
        };
        package.SessionSlots = BookingPaymentFlowTests.BuildSlots(package.Id, package.SessionCount);
        var settings = rate.HasValue
            ? new FakePlatformSettingRepository(rate.Value)
            : new FakePlatformSettingRepository(); // seeded default: 0%
        var bookings = new FakeBookingRepository();
        var svc = new BookingService(
            new FakeTrainingPackageRepository(package),
            bookings,
            new FakeTrainingSessionRepository(),
            new FakePaymentRepository(),
            new FakePayOsService(),
            new FakeCoachWalletRepository(),
            new FakeNotificationRepository(),
            settings,
            new FakeVoucherService(),
            new FakeBookingSessionUsageService(),
            NullLogger<BookingService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new FeatureOptions { EnableManualPurchase = true }),
            new PassValidator<PurchaseTrainingPackageManualRequest>(),
            new PassValidator<PurchaseTrainingPackagePayOsRequest>(),
            new PassValidator<BookingFilterRequest>());
        return (svc, settings, bookings, package.Id);
    }

    // Default (seeded) commission is 0%: the coach receives the full amount.
    [Fact]
    public async Task Booking_DefaultZeroCommission_CoachReceivesFullAmount()
    {
        var (svc, _, _, packageId) = Build(price: 1_000_000m, sessions: 4);

        var result = await svc.PurchaseManualAsync(Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageId });

        var b = result.Data!;
        Assert.Equal(1_000_000m, b.TotalAmount);
        Assert.Equal(0m, b.PlatformFeeRate);
        Assert.Equal(0m, b.PlatformFeeAmount);
        Assert.Equal(1_000_000m, b.CoachReceiveAmount);
        Assert.Equal(250_000m, b.PerSessionCoachAmount); // 1,000,000 / 4
    }

    // Admin-configured fractional percentage (12.5% = rate 0.125).
    [Fact]
    public async Task Booking_ConfiguredTwelvePointFivePercent_SnapshotsExactAmounts()
    {
        var (svc, _, _, packageId) = Build(price: 1_000_000m, sessions: 4, rate: 0.125m);

        var result = await svc.PurchaseManualAsync(Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageId });

        var b = result.Data!;
        Assert.Equal(0.125m, b.PlatformFeeRate);
        Assert.Equal(125_000m, b.PlatformFeeAmount);
        Assert.Equal(875_000m, b.CoachReceiveAmount);
        Assert.Equal(218_750m, b.PerSessionCoachAmount); // 875,000 / 4, exact
    }

    // Legacy 15% math is unchanged when 15% is the configured rate.
    [Fact]
    public async Task Booking_1MillionOver3Sessions_AppliesFifteenPercentFee()
    {
        var (svc, _, _, packageId) = Build(price: 1_000_000m, sessions: 3, rate: 0.15m);

        var result = await svc.PurchaseManualAsync(Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageId });

        var b = result.Data!;
        Assert.Equal(1_000_000m, b.TotalAmount);
        Assert.Equal(0.15m, b.PlatformFeeRate);
        Assert.Equal(150_000m, b.PlatformFeeAmount);   // 15%
        Assert.Equal(850_000m, b.CoachReceiveAmount);  // 85%
        // per session = 850,000 / 3 = 283,333.33… (rounded for display assertion only)
        Assert.Equal(283_333.33m, Math.Round(b.PerSessionCoachAmount, 2));
    }

    [Fact]
    public async Task Booking_EvenlyDivisible_PerSessionExact()
    {
        var (svc, _, _, packageId) = Build(price: 1_000_000m, sessions: 5, rate: 0.15m);

        var result = await svc.PurchaseManualAsync(Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageId });

        var b = result.Data!;
        Assert.Equal(150_000m, b.PlatformFeeAmount);
        Assert.Equal(850_000m, b.CoachReceiveAmount);
        Assert.Equal(170_000m, b.PerSessionCoachAmount); // 850,000 / 5, exact
    }

    // Snapshot immutability: changing the platform setting later never mutates an existing booking;
    // only bookings created AFTER the change use the new rate.
    [Fact]
    public async Task ChangingCommission_DoesNotMutateExistingBooking_AppliesToNextBooking()
    {
        var (svc, settings, bookings, packageId) = Build(price: 1_000_000m, sessions: 4, rate: 0.15m);

        await svc.PurchaseManualAsync(Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageId });
        var oldBooking = Assert.Single(bookings.Added);

        // Admin drops the commission to 0% — the historical snapshot must not move.
        settings.Setting.CommissionRate = 0m;

        Assert.Equal(0.15m, oldBooking.PlatformFeeRate);
        Assert.Equal(150_000m, oldBooking.PlatformFeeAmount);
        Assert.Equal(850_000m, oldBooking.CoachReceiveAmount);
        Assert.Equal(212_500m, oldBooking.PerSessionCoachAmount); // 850,000 / 4

        await svc.PurchaseManualAsync(Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageId });
        var newBooking = bookings.Added[1];

        Assert.Equal(0m, newBooking.PlatformFeeRate);
        Assert.Equal(0m, newBooking.PlatformFeeAmount);
        Assert.Equal(1_000_000m, newBooking.CoachReceiveAmount);
        Assert.Equal(250_000m, newBooking.PerSessionCoachAmount);

        // And the old booking is still untouched after the second purchase.
        Assert.Equal(0.15m, oldBooking.PlatformFeeRate);
        Assert.Equal(150_000m, oldBooking.PlatformFeeAmount);
    }
}
