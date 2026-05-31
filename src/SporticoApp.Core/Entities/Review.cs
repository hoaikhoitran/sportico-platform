using System;
using System.Collections.Generic;
using SporticoApp.Shared.Constants;

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

    /// <summary>The successful booking that entitled this review (audit trail of eligibility).</summary>
    public Guid? BookingId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    /// <summary>active | hidden | deleted. Only <c>active</c> reviews are public and counted in rating stats.</summary>
    public string Status { get; set; } = ReviewStatuses.Active;

    public DateTime? DeletedAt { get; set; }

    /// <summary>User (learner self-delete or admin) who hid/deleted the review.</summary>
    public Guid? DeletedByUserId { get; set; }

    /// <summary>Reason recorded when admin hides/deletes a review via moderation.</summary>
    public string? ModerationReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual User learner { get; set; } = null!;

    public virtual Post? Post { get; set; }

    public virtual Booking? Booking { get; set; }
}
