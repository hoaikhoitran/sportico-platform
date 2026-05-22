using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Lịch sử mua gói của Coach
/// </summary>
public partial class CoachPackage
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    public int PackageId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int RemainingPosts { get; set; }

    /// <summary>
    /// pending | active | expired | cancelled
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual Package Package { get; set; } = null!;
}
