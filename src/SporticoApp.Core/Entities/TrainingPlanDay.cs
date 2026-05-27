using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Day plan breakdown.
/// </summary>
public partial class TrainingPlanDay
{
    public Guid Id { get; set; }

    public Guid TrainingPlanWeekId { get; set; }

    public int DayNumber { get; set; }

    public string Title { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual TrainingPlanWeek TrainingPlanWeek { get; set; } = null!;

    public virtual ICollection<TrainingPlanExercise> Exercises { get; set; } = new List<TrainingPlanExercise>();
}
