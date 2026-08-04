using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Vouchers;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Vouchers;

/// <summary>
/// Covers voucher quote/reserve/apply/release business rules (Part A of the voucher feature):
/// discount formulas, eligibility gates, quota (reserved counts toward the limit), budget, and the
/// guarded reserved→applied/released state machine that keeps webhook + reconcile idempotent.
/// </summary>
public class VoucherServiceTests
{
    private static readonly Guid LearnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static (VoucherService Svc, FakeVoucherCampaignRepository Campaigns, FakeVoucherRedemptionRepository Redemptions)
        Build()
    {
        var campaigns = new FakeVoucherCampaignRepository();
        var redemptions = new FakeVoucherRedemptionRepository();
        var packages = new FakeVoucherTrainingPackageRepository();
        var svc = new VoucherService(
            campaigns,
            redemptions,
            packages,
            new ValidateVoucherRequestValidator(),
            new CreateVoucherCampaignRequestValidator(),
            new UpdateVoucherCampaignRequestValidator(),
            new VoucherCampaignFilterRequestValidator(),
            new VoucherRedemptionFilterRequestValidator());
        return (svc, campaigns, redemptions);
    }

    private static VoucherCampaign ActiveCampaign(
        string discountType = VoucherDiscountTypes.FixedAmount,
        decimal discountValue = 100_000m,
        decimal? maxDiscount = null,
        decimal? minOrder = null,
        int? maxUsesTotal = null,
        int? maxUsesPerLearner = null,
        decimal? budget = null)
    {
        var now = DateTime.UtcNow;
        return new VoucherCampaign
        {
            Id = Guid.NewGuid(),
            Code = "WELCOME10",
            Name = "Welcome",
            DiscountType = discountType,
            DiscountValue = discountValue,
            MaxDiscountAmount = maxDiscount,
            MinOrderAmount = minOrder,
            Status = VoucherCampaignStatuses.Active,
            MaxUsesTotal = maxUsesTotal,
            MaxUsesPerLearner = maxUsesPerLearner,
            BudgetAmount = budget,
            ReservedCount = 0,
            UsedCount = 0,
            ReservedDiscountAmount = 0,
            UsedDiscountAmount = 0,
            CreatedByUserId = AdminId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    // ── Discount formulas ────────────────────────────────────────────────────

    [Fact]
    public async Task Reserve_FixedAmount_DiscountsExactAmount()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.FixedAmount, 100_000m);
        campaigns.Campaigns[campaign.Id] = campaign;

        var reservation = await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid());

        Assert.NotNull(reservation);
        Assert.Equal(100_000m, reservation!.DiscountAmount);
    }

    [Fact]
    public async Task Reserve_FixedAmount_NeverExceedsOriginalAmount()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.FixedAmount, 2_000_000m); // bigger than the price
        campaigns.Campaigns[campaign.Id] = campaign;

        var reservation = await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid());

        Assert.Equal(1_000_000m, reservation!.DiscountAmount); // capped at the order total, not negative
    }

    [Fact]
    public async Task Reserve_Percentage_WithMaxDiscount_CapsAtMax()
    {
        var (svc, campaigns, _) = Build();
        // 20% of 1,000,000 = 200,000, but capped at 100,000.
        var campaign = ActiveCampaign(VoucherDiscountTypes.Percentage, 20m, maxDiscount: 100_000m);
        campaigns.Campaigns[campaign.Id] = campaign;

        var reservation = await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid());

        Assert.Equal(100_000m, reservation!.DiscountAmount);
    }

    [Fact]
    public async Task Reserve_Percentage_WithoutMaxDiscount_UsesFullPercentage()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.Percentage, 10m);
        campaigns.Campaigns[campaign.Id] = campaign;

        var reservation = await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid());

        Assert.Equal(100_000m, reservation!.DiscountAmount); // 10% of 1,000,000
    }

    // ── Eligibility gates ────────────────────────────────────────────────────

    [Fact]
    public async Task Reserve_MinOrderNotMet_Throws()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(minOrder: 2_000_000m);
        campaigns.Campaigns[campaign.Id] = campaign;

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid()));

        Assert.Equal(ErrorCodes.VoucherMinOrderNotMet, ex.Code);
    }

    [Fact]
    public async Task Reserve_NotStartedYet_Throws()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign();
        campaign.StartAt = DateTime.UtcNow.AddDays(1);
        campaigns.Campaigns[campaign.Id] = campaign;

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid()));

        Assert.Equal(ErrorCodes.VoucherNotStarted, ex.Code);
    }

    [Fact]
    public async Task Reserve_Expired_Throws()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign();
        campaign.EndAt = DateTime.UtcNow.AddDays(-1);
        campaigns.Campaigns[campaign.Id] = campaign;

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid()));

        Assert.Equal(ErrorCodes.VoucherExpired, ex.Code);
    }

    [Fact]
    public async Task Reserve_Paused_Throws()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign();
        campaign.Status = VoucherCampaignStatuses.Paused;
        campaigns.Campaigns[campaign.Id] = campaign;

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid()));

        Assert.Equal(ErrorCodes.VoucherNotActive, ex.Code);
    }

    [Fact]
    public async Task Reserve_UnknownCode_ThrowsNotFound()
    {
        var (svc, _, _) = Build();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.ReserveForBookingAsync(LearnerId, "DOES-NOT-EXIST", Guid.NewGuid(), 1_000_000m, Guid.NewGuid()));
    }

    [Fact]
    public async Task Reserve_NullOrBlankCode_ReturnsNull_NoReservationCreated()
    {
        var (svc, _, redemptions) = Build();

        var result = await svc.ReserveForBookingAsync(LearnerId, null, Guid.NewGuid(), 1_000_000m, Guid.NewGuid());

        Assert.Null(result);
        Assert.Empty(redemptions.Redemptions);
    }

    // ── Quota (reserved counts toward the limit) ────────────────────────────

    [Fact]
    public async Task Reserve_MaxUsesTotalReached_IncludingReservedOnes_Throws()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(maxUsesTotal: 1);
        campaigns.Campaigns[campaign.Id] = campaign;

        // First reservation succeeds and consumes the only slot (still "reserved", not "applied").
        await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid());

        // A second learner racing for the last slot must be rejected — reserved already counts.
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            svc.ReserveForBookingAsync(Guid.NewGuid(), "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid()));

        Assert.Equal(ErrorCodes.VoucherUsageLimitReached, ex.Code);
    }

    [Fact]
    public async Task Reserve_MaxUsesPerLearnerReached_Throws()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(maxUsesPerLearner: 1);
        campaigns.Campaigns[campaign.Id] = campaign;

        await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid()));

        Assert.Equal(ErrorCodes.VoucherLearnerLimitReached, ex.Code);
    }

    [Fact]
    public async Task Reserve_BudgetExceeded_Throws()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.FixedAmount, 100_000m, budget: 150_000m);
        campaigns.Campaigns[campaign.Id] = campaign;

        // First reservation uses 100,000 of the 150,000 budget.
        await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid());

        // Second would need another 100,000 -> 200,000 total > 150,000 budget.
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            svc.ReserveForBookingAsync(Guid.NewGuid(), "WELCOME10", Guid.NewGuid(), 1_000_000m, Guid.NewGuid()));

        Assert.Equal(ErrorCodes.VoucherBudgetExceeded, ex.Code);
    }

    // ── Reserve → apply/release state machine ───────────────────────────────

    [Fact]
    public async Task Reserve_IncrementsReservedCountAndReservedDiscountAmount()
    {
        var (svc, campaigns, redemptions) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.FixedAmount, 100_000m);
        campaigns.Campaigns[campaign.Id] = campaign;
        var bookingId = Guid.NewGuid();

        await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, bookingId);

        Assert.Equal(1, campaign.ReservedCount);
        Assert.Equal(100_000m, campaign.ReservedDiscountAmount);
        Assert.Equal(0, campaign.UsedCount);
        var redemption = Assert.Single(redemptions.Redemptions);
        Assert.Equal(VoucherRedemptionStatuses.Reserved, redemption.Status);
        Assert.Equal(bookingId, redemption.BookingId);
    }

    [Fact]
    public async Task Apply_TransitionsReservedToApplied_MovesCounters()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.FixedAmount, 100_000m);
        campaigns.Campaigns[campaign.Id] = campaign;
        var bookingId = Guid.NewGuid();
        await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, bookingId);

        await svc.ApplyForBookingAsync(bookingId, Guid.NewGuid());

        Assert.Equal(0, campaign.ReservedCount);
        Assert.Equal(1, campaign.UsedCount);
        Assert.Equal(0m, campaign.ReservedDiscountAmount);
        Assert.Equal(100_000m, campaign.UsedDiscountAmount);
    }

    [Fact]
    public async Task Apply_CalledTwice_IsIdempotent_AppliesOnlyOnce()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.FixedAmount, 100_000m);
        campaigns.Campaigns[campaign.Id] = campaign;
        var bookingId = Guid.NewGuid();
        await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, bookingId);

        // Simulates webhook AND reconcile both firing for the same paid payment.
        await svc.ApplyForBookingAsync(bookingId, Guid.NewGuid());
        await svc.ApplyForBookingAsync(bookingId, Guid.NewGuid());

        Assert.Equal(1, campaign.UsedCount);
        Assert.Equal(100_000m, campaign.UsedDiscountAmount);
    }

    [Fact]
    public async Task Release_TransitionsReservedToReleased_ReturnsCounters()
    {
        var (svc, campaigns, redemptions) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.FixedAmount, 100_000m);
        campaigns.Campaigns[campaign.Id] = campaign;
        var bookingId = Guid.NewGuid();
        await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, bookingId);

        await svc.ReleaseForBookingAsync(bookingId, "payment_cancelled");

        Assert.Equal(0, campaign.ReservedCount);
        Assert.Equal(0m, campaign.ReservedDiscountAmount);
        Assert.Equal(0, campaign.UsedCount); // never became used
        var redemption = Assert.Single(redemptions.Redemptions);
        Assert.Equal(VoucherRedemptionStatuses.Released, redemption.Status);
        Assert.Equal("payment_cancelled", redemption.ReleaseReason);
    }

    [Fact]
    public async Task Release_CalledTwice_DoesNotMakeCountersNegative()
    {
        var (svc, campaigns, _) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.FixedAmount, 100_000m);
        campaigns.Campaigns[campaign.Id] = campaign;
        var bookingId = Guid.NewGuid();
        await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, bookingId);

        await svc.ReleaseForBookingAsync(bookingId, "payment_cancelled");
        await svc.ReleaseForBookingAsync(bookingId, "payment_cancelled"); // idempotent no-op

        Assert.Equal(0, campaign.ReservedCount);
        Assert.Equal(0m, campaign.ReservedDiscountAmount);
    }

    [Fact]
    public async Task Release_NeverReleasesAnAppliedRedemption()
    {
        var (svc, campaigns, redemptions) = Build();
        var campaign = ActiveCampaign(VoucherDiscountTypes.FixedAmount, 100_000m);
        campaigns.Campaigns[campaign.Id] = campaign;
        var bookingId = Guid.NewGuid();
        await svc.ReserveForBookingAsync(LearnerId, "WELCOME10", Guid.NewGuid(), 1_000_000m, bookingId);
        await svc.ApplyForBookingAsync(bookingId, Guid.NewGuid());

        // A stray release call (e.g. a race with the expiry sweep) must never undo an applied use.
        await svc.ReleaseForBookingAsync(bookingId, "reservation_expired");

        Assert.Equal(VoucherRedemptionStatuses.Applied, redemptions.Redemptions.Single().Status);
        Assert.Equal(1, campaign.UsedCount);
        Assert.Equal(100_000m, campaign.UsedDiscountAmount);
    }

    [Fact]
    public async Task Release_UnknownBooking_IsNoOp()
    {
        var (svc, _, _) = Build();

        // No exception, no-op — the booking never had a voucher.
        await svc.ReleaseForBookingAsync(Guid.NewGuid(), "payment_cancelled");
    }
}
