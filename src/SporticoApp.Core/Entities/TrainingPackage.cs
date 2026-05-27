using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Training package created by coach for learners.
/// </summary>
public partial class TrainingPackage
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    public int SportId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int SessionCount { get; set; }

    public int DurationDays { get; set; }

    public string? Location { get; set; }

    public bool IsOnline { get; set; }

    public string? Level { get; set; }

    public string? GoalType { get; set; }

    /// <summary>
    /// pending | published | rejected | archived
    /// </summary>
    public string Status { get; set; } = null!;

    public string? RejectionReason { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual Sport Sport { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
