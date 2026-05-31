using System;
using System.Collections.Generic;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Báo cáo vi phạm
/// </summary>
public partial class Report
{
    public Guid Id { get; set; }

    public Guid reporter_id { get; set; }

    /// <summary>
    /// Legacy direct user target. Nullable now that reports can target reviews;
    /// use <see cref="TargetType"/>/<see cref="TargetId"/> for new reports.
    /// </summary>
    public Guid? target_user_id { get; set; }

    /// <summary>user | review. Defaults to user for backward compatibility.</summary>
    public string TargetType { get; set; } = ReportTargetTypes.User;

    /// <summary>Id of the reported entity (user id or review id).</summary>
    public Guid? TargetId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// pending | reviewing | resolved | rejected
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>Admin who resolved/rejected the report.</summary>
    public Guid? HandledByUserId { get; set; }

    public DateTime? HandledAt { get; set; }

    public string? ResolutionNote { get; set; }

    /// <summary>none | review_hidden | review_deleted — moderation action taken on resolve.</summary>
    public string? ActionTaken { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User reporter { get; set; } = null!;

    public virtual User? target_user { get; set; }
}
