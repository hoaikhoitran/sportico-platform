using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Assessment collected from learner for personalization.
/// </summary>
public partial class LearnerAssessment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid LearnerId { get; set; }

    public Guid CoachId { get; set; }

    public string GoalType { get; set; } = null!;

    public string? GoalDescription { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? BodyFatPercent { get; set; }

    public string? CurrentLevel { get; set; }

    public string? HealthNotes { get; set; }

    public string? InjuryNotes { get; set; }

    public string? TrainingHistory { get; set; }

    public string? AvailableDaysPerWeek { get; set; }

    public int? PreferredSessionDurationMinutes { get; set; }

    public string? EquipmentAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User Learner { get; set; } = null!;

    public virtual CoachProfile Coach { get; set; } = null!;
}
