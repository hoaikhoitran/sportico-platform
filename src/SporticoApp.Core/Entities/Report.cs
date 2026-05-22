using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Báo cáo vi phạm
/// </summary>
public partial class Report
{
    public Guid Id { get; set; }

    public Guid reporter_id { get; set; }

    public Guid target_user_id { get; set; }

    public string Reason { get; set; } = null!;

    /// <summary>
    /// pending | reviewing | resolved | rejected
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User reporter { get; set; } = null!;

    public virtual User target_user { get; set; } = null!;
}
