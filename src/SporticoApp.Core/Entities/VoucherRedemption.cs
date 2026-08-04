using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// One learner's use of one <see cref="VoucherCampaign"/> against exactly one <see cref="Booking"/>.
/// Lifecycle: reserved (payment pending) → applied (payment paid, permanent) or released
/// (payment cancelled/failed/expired). Applied and released are terminal — see
/// <see cref="Shared.Constants.VoucherRedemptionStatuses"/> and VoucherService for the guarded
/// transitions that keep webhook + reconcile idempotent.
/// </summary>
public partial class VoucherRedemption
{
    public Guid Id { get; set; }

    public Guid VoucherCampaignId { get; set; }

    /// <summary>Unique — a booking can hold at most one voucher redemption.</summary>
    public Guid BookingId { get; set; }

    public Guid LearnerId { get; set; }

    public Guid? PaymentId { get; set; }

    /// <summary>reserved | applied | released.</summary>
    public string Status { get; set; } = null!;

    /// <summary>Snapshot of TrainingPackage.Price at reservation time.</summary>
    public decimal OriginalAmount { get; set; }

    /// <summary>Snapshot of the computed discount at reservation time.</summary>
    public decimal DiscountAmount { get; set; }

    public DateTime ReservedAt { get; set; }

    /// <summary>When a still-reserved redemption becomes eligible for the release sweep.</summary>
    public DateTime? ExpiresAt { get; set; }

    public DateTime? AppliedAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    /// <summary>payment_cancelled | payment_failed | payment_expired | payos_link_creation_failed.</summary>
    public string? ReleaseReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int Version { get; set; }

    public virtual VoucherCampaign VoucherCampaign { get; set; } = null!;

    public virtual Booking Booking { get; set; } = null!;

    public virtual User Learner { get; set; } = null!;
}
