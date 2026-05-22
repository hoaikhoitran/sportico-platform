using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Hồ sơ huấn luyện viên
/// </summary>
public partial class CoachProfile
{
    public Guid UserId { get; set; }

    public string? Bio { get; set; }

    public int? ExperienceYears { get; set; }

    public string? Headline { get; set; }

    /// <summary>
    /// Cache: trung bình Rating từ bảng Review
    /// </summary>
    public decimal Rating { get; set; }

    /// <summary>
    /// Cache: tổng số review
    /// </summary>
    public int TotalReviews { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CoachPackage> CoachPackages { get; set; } = new List<CoachPackage>();

    public virtual ICollection<CoachSport> CoachSports { get; set; } = new List<CoachSport>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual User User { get; set; } = null!;
}
