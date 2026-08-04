using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Platform-funded discount campaign for <see cref="TrainingPackage"/> purchases, created and
/// managed by admins. A learner redeems one campaign per booking via <see cref="VoucherRedemption"/>.
/// Discounts are always funded by the platform: they reduce PlatformNetRevenue only, never
/// Booking.CoachReceiveAmount / PerSessionCoachAmount (see BookingService.CreateBookingSnapshot).
/// </summary>
public partial class VoucherCampaign
{
    public Guid Id { get; set; }

    /// <summary>Learner-facing redemption code. Stored case-insensitively (citext).</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>fixed_amount | percentage — see <see cref="Shared.Constants.VoucherDiscountTypes"/>.</summary>
    public string DiscountType { get; set; } = null!;

    /// <summary>Fixed-amount: currency units. Percentage: 0..100.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Percentage-type cap on the discount amount. Ignored for fixed_amount.</summary>
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>Minimum TrainingPackage.Price required to redeem this voucher.</summary>
    public decimal? MinOrderAmount { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    /// <summary>draft | active | paused | ended — see <see cref="Shared.Constants.VoucherCampaignStatuses"/>.</summary>
    public string Status { get; set; } = null!;

    public int? MaxUsesTotal { get; set; }

    public int? MaxUsesPerLearner { get; set; }

    /// <summary>Redemptions currently held by a pending PayOS payment (reserved, not yet applied).</summary>
    public int ReservedCount { get; set; }

    /// <summary>Redemptions that reached a paid booking (permanent).</summary>
    public int UsedCount { get; set; }

    /// <summary>Optional total discount budget in currency units. Null = unlimited.</summary>
    public decimal? BudgetAmount { get; set; }

    /// <summary>Sum of DiscountAmount currently held by reserved redemptions.</summary>
    public decimal ReservedDiscountAmount { get; set; }

    /// <summary>Sum of DiscountAmount permanently consumed by applied redemptions.</summary>
    public decimal UsedDiscountAmount { get; set; }

    /// <summary>Optimistic concurrency token — guards the reserve/apply/release counters.</summary>
    public int Version { get; set; }

    public Guid CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<VoucherRedemption> Redemptions { get; set; } = new List<VoucherRedemption>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
