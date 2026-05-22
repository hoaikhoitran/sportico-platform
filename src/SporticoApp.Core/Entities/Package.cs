using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Gói dịch vụ dành cho Coach (basic, pro, premium...)
/// </summary>
public partial class Package
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int DurationDays { get; set; }

    public int MaxPosts { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CoachPackage> CoachPackages { get; set; } = new List<CoachPackage>();
}
