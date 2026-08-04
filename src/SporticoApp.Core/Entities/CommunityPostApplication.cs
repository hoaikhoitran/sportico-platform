using System;

namespace SporticoApp.Core.Entities;

/// <summary>A user's request to join a recruitment-type <see cref="CommunityPost"/>. One row per (post, applicant).</summary>
public partial class CommunityPostApplication
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Guid ApplicantId { get; set; }

    public string? Message { get; set; }

    /// <summary>pending | accepted | rejected | cancelled.</summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public Guid? RespondedByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }

    public virtual CommunityPost Post { get; set; } = null!;

    public virtual User Applicant { get; set; } = null!;
}
