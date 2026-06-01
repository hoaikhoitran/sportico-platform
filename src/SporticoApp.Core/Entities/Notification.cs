using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Thông báo cho User
/// </summary>
public partial class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    /// <summary>
    /// One of <see cref="SporticoApp.Shared.Constants.NotificationTypeConstants"/>:
    /// message | review | follow | payment | package | post | system | report |
    /// booking | training_package | training_session | training_plan | wallet
    /// </summary>
    public string Type { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
