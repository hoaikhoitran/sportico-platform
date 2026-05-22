using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Hồ sơ học viên
/// </summary>
public partial class LearnerProfile
{
    public Guid UserId { get; set; }

    public string? Goal { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
