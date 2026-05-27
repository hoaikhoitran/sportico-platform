using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Weekly plan breakdown.
/// </summary>
public partial class TrainingPlanWeek
{
    public Guid Id { get; set; }

    public Guid TrainingPlanId { get; set; }

    public int WeekNumber { get; set; }

    public string? Focus { get; set; }

    public string? Notes { get; set; }

    public virtual TrainingPlan TrainingPlan { get; set; } = null!;

    public virtual ICollection<TrainingPlanDay> Days { get; set; } = new List<TrainingPlanDay>();
}
