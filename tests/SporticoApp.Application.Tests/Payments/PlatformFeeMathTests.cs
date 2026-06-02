using Microsoft.Extensions.Logging.Abstractions;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.Options;
using SporticoApp.Application.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>
/// Part F: 15% platform fee is applied once at booking purchase. Coach receives 85%, split
/// evenly per session. Rounding policy: amounts are stored at full decimal precision (no
/// premature rounding); per-session is coachReceive / totalSessions.
/// </summary>
public class PlatformFeeMathTests
{
    private static readonly Guid Learner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Coach = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static (BookingService svc, Guid packageId) Build(decimal price, int sessions)
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
        var svc = new BookingService(
            new FakeTrainingPackageRepository(package),
            new FakeBookingRepository(),
            new FakePaymentRepository(),
            new FakePayOsService(),
            new FakeCoachWalletRepository(),
            new FakeNotificationRepository(),
            NullLogger<BookingService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new FeatureOptions { EnableManualPurchase = true }),
            new PassValidator<PurchaseTrainingPackageManualRequest>(),
            new PassValidator<PurchaseTrainingPackagePayOsRequest>(),
            new PassValidator<BookingFilterRequest>());
        return (svc, package.Id);
    }

    [Fact]
    public async Task Booking_1MillionOver3Sessions_AppliesFifteenPercentFee()
    {
        var (svc, packageId) = Build(price: 1_000_000m, sessions: 3);

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
        var (svc, packageId) = Build(price: 1_000_000m, sessions: 5);

        var result = await svc.PurchaseManualAsync(Learner, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageId });

        var b = result.Data!;
        Assert.Equal(150_000m, b.PlatformFeeAmount);
        Assert.Equal(850_000m, b.CoachReceiveAmount);
        Assert.Equal(170_000m, b.PerSessionCoachAmount); // 850,000 / 5, exact
    }
}
