using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Exercise details for a training plan day.
/// </summary>
public partial class TrainingPlanExercise
{
    public Guid Id { get; set; }

    public Guid TrainingPlanDayId { get; set; }

    public string ExerciseName { get; set; } = null!;

    public int OrderIndex { get; set; }

    public int? Sets { get; set; }

    public string? Reps { get; set; }

    public string? Intensity { get; set; }

    public int? RestSeconds { get; set; }

    public string? Notes { get; set; }

    public virtual TrainingPlanDay TrainingPlanDay { get; set; } = null!;
}
