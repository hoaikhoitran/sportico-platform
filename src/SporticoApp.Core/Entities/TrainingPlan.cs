using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Training plan for a booking.
/// </summary>
public partial class TrainingPlan
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid LearnerId { get; set; }

    public Guid CoachId { get; set; }

    public string Title { get; set; } = null!;

    public string GoalType { get; set; } = null!;

    public string? Overview { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalWeeks { get; set; }

    /// <summary>
    /// draft | active | completed | cancelled
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User Learner { get; set; } = null!;

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual ICollection<TrainingPlanWeek> Weeks { get; set; } = new List<TrainingPlanWeek>();
}
