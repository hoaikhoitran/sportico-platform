using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// A single scheduled session defined by the coach as part of a <see cref="TrainingPackage"/>.
/// The full schedule is fixed at package-creation time. When a learner purchases the package,
/// one <see cref="TrainingSession"/> is auto-generated per slot and a seat is consumed here.
/// </summary>
public partial class TrainingPackageSessionSlot
{
    public Guid Id { get; set; }

    public Guid TrainingPackageId { get; set; }

    /// <summary>1..SessionCount — unique and contiguous within the owning package.</summary>
    public int SessionNumber { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string? Level { get; set; }

    public string? Location { get; set; }

    public bool IsOnline { get; set; }

    public string? MeetingUrl { get; set; }

    public string? Note { get; set; }

    /// <summary>Maximum learners that can buy a seat on this session slot (group sessions). Must be &gt; 0.</summary>
    public int MaxParticipants { get; set; } = 1;

    /// <summary>Seats consumed/reserved by purchases. Never exceeds <see cref="MaxParticipants"/>.</summary>
    public int BookedParticipants { get; set; }

    /// <summary>open | full | cancelled</summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Application-managed optimistic concurrency token (same pattern as
    /// <see cref="CoachAvailabilitySlot.Version"/>). Incremented on every reserve/release so two
    /// learners buying the last seat of the same slot concurrently cannot both commit — the second
    /// SaveChanges raises a concurrency conflict (surfaced as a 409).
    /// </summary>
    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual TrainingPackage TrainingPackage { get; set; } = null!;
}
