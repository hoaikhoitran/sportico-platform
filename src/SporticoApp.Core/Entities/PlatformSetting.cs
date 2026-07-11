using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Singleton platform-wide settings row (exactly one row, keyed by <see cref="SingletonId"/>).
/// Holds the editable platform commission applied to NEW bookings. A booking's own snapshotted
/// PlatformFeeRate remains the immutable historical source of truth after creation.
/// </summary>
public partial class PlatformSetting
{
    /// <summary>
    /// Fixed id of the single authoritative settings row. Seeded by migration; also makes
    /// concurrent create-if-missing race-safe (the second insert violates the primary key).
    /// </summary>
    public static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; }

    /// <summary>
    /// Fractional commission rate applied to new bookings: 0.0000 (0%) .. 1.0000 (100%).
    /// </summary>
    public decimal CommissionRate { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Application-managed optimistic concurrency token (same pattern as CoachWallet.Version):
    /// two admins saving the settings concurrently cannot both commit silently.
    /// </summary>
    public int Version { get; set; }
}
