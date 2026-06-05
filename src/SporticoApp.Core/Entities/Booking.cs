using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Booking for a training package purchase.
/// </summary>
public partial class Booking
{
    public Guid Id { get; set; }

    public Guid LearnerId { get; set; }

    public Guid CoachId { get; set; }

    public Guid TrainingPackageId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PlatformFeeRate { get; set; }

    public decimal PlatformFeeAmount { get; set; }

    public decimal CoachReceiveAmount { get; set; }

    public decimal PerSessionCoachAmount { get; set; }

    public int TotalSessions { get; set; }

    public int CompletedSessions { get; set; }

    /// <summary>
    /// pending_payment | active | completed | cancelled | refunded
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    /// <summary>Set when the booking becomes active: PaidAt + TrainingPackage.DurationDays.</summary>
    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Application-managed optimistic concurrency token, bumped whenever a training session is
    /// created against this booking (and on completion). Prevents two concurrent create-session
    /// requests from both passing the quota check and exceeding TotalSessions.
    /// </summary>
    public int Version { get; set; }

    public virtual User Learner { get; set; } = null!;

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual TrainingPackage TrainingPackage { get; set; } = null!;

    public virtual ICollection<TrainingSession> TrainingSessions { get; set; } = new List<TrainingSession>();

    public virtual LearnerAssessment? LearnerAssessment { get; set; }

    public virtual TrainingPlan? TrainingPlan { get; set; }
}
