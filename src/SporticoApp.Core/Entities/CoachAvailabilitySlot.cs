namespace SporticoApp.Core.Entities;

/// <summary>A time slot published by a coach as available for booking.</summary>
public class CoachAvailabilitySlot
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    /// <summary>available | booked | cancelled | expired</summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Maximum number of learners that can book this (group) slot. Each active training session
    /// consumes one seat. Defaults to 1 (a private slot — the original behaviour).
    /// </summary>
    public int MaxParticipants { get; set; } = 1;

    public string? Location { get; set; }

    public string? MeetingUrl { get; set; }

    public bool IsOnline { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Application-managed optimistic concurrency token. Incremented on every booking/release so two
    /// learners booking the last seat of the same slot concurrently cannot both succeed — the second
    /// SaveChanges raises a concurrency conflict (surfaced as a 409). Replaces the old filtered unique
    /// index that capped a slot at one active session.
    /// </summary>
    public int Version { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;
}
