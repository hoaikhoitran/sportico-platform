using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Scheduled training session for a booking.
/// </summary>
public partial class TrainingSession
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid LearnerId { get; set; }

    public Guid CoachId { get; set; }

    /// <summary>The availability slot this session was booked from. Nullable for legacy records.</summary>
    public Guid? AvailabilitySlotId { get; set; }

    public virtual CoachAvailabilitySlot? AvailabilitySlot { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    /// <summary>
    /// requested | scheduled | completed | cancelled | missed
    /// </summary>
    public string Status { get; set; } = null!;

    public string? MeetingUrl { get; set; }

    public string? Location { get; set; }

    public string? LearnerNote { get; set; }

    public string? CoachNote { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User Learner { get; set; } = null!;

    public virtual CoachProfile Coach { get; set; } = null!;
}
