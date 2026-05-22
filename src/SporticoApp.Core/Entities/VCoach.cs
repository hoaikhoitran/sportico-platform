using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

public partial class VCoach
{
    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Status { get; set; }

    public string? Bio { get; set; }

    public string? Headline { get; set; }

    public int? ExperienceYears { get; set; }

    public decimal? Rating { get; set; }

    public int? TotalReviews { get; set; }

    public DateTime? CoachSince { get; set; }

    public List<string>? Sports { get; set; }
}
