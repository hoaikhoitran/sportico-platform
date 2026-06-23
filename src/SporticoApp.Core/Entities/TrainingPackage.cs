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

    /// <summary>
    /// Legacy-compatible field. The new flow is start/end-date based; this is derived from
    /// <see cref="StartDate"/>..<see cref="EndDate"/> on create/update and kept only so existing
    /// booking-expiry logic and old data continue to work.
    /// </summary>
    public int DurationDays { get; set; }

    /// <summary>First calendar day the package schedule may span (new start/end-date model).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Last calendar day the package schedule may span (new start/end-date model).</summary>
    public DateTime EndDate { get; set; }

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

    /// <summary>The fixed schedule of sessions the coach defined when creating the package.</summary>
    public virtual ICollection<TrainingPackageSessionSlot> SessionSlots { get; set; }
        = new List<TrainingPackageSessionSlot>();
}
