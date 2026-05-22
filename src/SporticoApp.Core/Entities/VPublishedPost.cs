using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

public partial class VPublishedPost
{
    public Guid? Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public string? Location { get; set; }

    public bool? IsOnline { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? CoachId { get; set; }

    public string? CoachName { get; set; }

    public string? CoachAvatar { get; set; }

    public decimal? CoachRating { get; set; }

    public int? CoachTotalReviews { get; set; }

    public int? SportId { get; set; }

    public string? SportName { get; set; }

    public string? SportSlug { get; set; }
}
