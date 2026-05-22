using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Đánh giá từ learner cho Coach
/// </summary>
public partial class Review
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    public Guid learner_id { get; set; }

    public Guid? PostId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual User learner { get; set; } = null!;

    public virtual Post? Post { get; set; }
}
