using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Progress check-in for a booking.
/// </summary>
public partial class ProgressCheckIn
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid LearnerId { get; set; }

    public Guid CoachId { get; set; }

    public DateTime CheckInDate { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? BodyFatPercent { get; set; }

    public decimal? WaistCm { get; set; }

    public string? EnergyLevel { get; set; }

    public string? SleepQuality { get; set; }

    public string? LearnerNote { get; set; }

    public string? CoachFeedback { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User Learner { get; set; } = null!;

    public virtual CoachProfile Coach { get; set; } = null!;
}
