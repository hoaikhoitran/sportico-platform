using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Optional teaching location where a coach can teach offline.
/// A coach can have multiple locations; one may be marked as default.
/// </summary>
public partial class CoachTeachingLocation
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    public string Address { get; set; } = null!;

    public string? City { get; set; }

    public string? District { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;
}
