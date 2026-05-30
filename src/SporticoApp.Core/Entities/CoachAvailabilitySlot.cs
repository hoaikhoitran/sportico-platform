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

    public string? Location { get; set; }

    public string? MeetingUrl { get; set; }

    public bool IsOnline { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;
}
