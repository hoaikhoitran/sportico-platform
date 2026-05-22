using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Danh mục môn thể thao
/// </summary>
public partial class Sport
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>
    /// URL-friendly identifier, vd: cau-long, bong-da
    /// </summary>
    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CoachSport> CoachSports { get; set; } = new List<CoachSport>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}
