using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.Options;
using SporticoApp.Application.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Enums;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>
/// Part B: the dev/test manual-purchase endpoint must be disabled by default
/// (Features:EnableManualPurchase=false) and return a clean business error — not a 500 —
/// while staying usable when explicitly enabled for dev/test.
/// </summary>
public class ManualPurchaseGuardTests
{
    private static readonly Guid LearnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CoachId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static (BookingService svc, FakeBookingRepository bookings, Guid packageId) Build(bool enableManual)
    {
        var package = new TrainingPackage
        {
            Id = Guid.NewGuid(),
            CoachId = CoachId,
            DurationDays = 30,
            SessionCount = 3,
            Price = 2000m,
            Status = TrainingPackageStatuses.Published
        };
        var bookings = new FakeBookingRepository();
        var svc = new BookingService(
            new FakeTrainingPackageRepository(package),
            bookings,
            new FakePaymentRepository(),
            new FakePayOsService(),
            new FakeCoachWalletRepository(),
            new FakeNotificationRepository(),
            new FakeBookingSessionUsageService(),
            NullLogger<BookingService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new FeatureOptions { EnableManualPurchase = enableManual }),
            new PassValidator<PurchaseTrainingPackageManualRequest>(),
            new PassValidator<PurchaseTrainingPackagePayOsRequest>(),
            new PassValidator<BookingFilterRequest>());

        return (svc, bookings, package.Id);
    }

    [Fact]
    public async Task ManualPurchase_Disabled_ThrowsForbiddenBusinessError()
    {
        var (svc, bookings, packageId) = Build(enableManual: false);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.PurchaseManualAsync(LearnerId, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageId }));

        Assert.Equal(ErrorCodes.ManualPurchaseDisabled, ex.Code);
        Assert.Equal(ErrorType.Forbidden, ex.Type); // maps to 403, not 500
        Assert.Empty(bookings.Added);               // no booking created
    }

    [Fact]
    public async Task ManualPurchase_Enabled_CreatesActiveBooking()
    {
        var (svc, bookings, packageId) = Build(enableManual: true);

        var result = await svc.PurchaseManualAsync(LearnerId, new PurchaseTrainingPackageManualRequest { TrainingPackageId = packageId });

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatuses.Active, result.Data!.Status);
        Assert.NotNull(result.Data.PaidAt);
        var created = Assert.Single(bookings.Added);
        Assert.Equal(BookingStatuses.Active, created.Status);
    }
}
