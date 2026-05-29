using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Media item (image URL) attached to a coach profile:
/// certificate, award, gallery, identity or other.
/// Stores URLs only, never binary content.
/// </summary>
public partial class CoachProfileMedia
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    /// <summary>
    /// certificate | award | gallery | identity | other
    /// </summary>
    public string MediaType { get; set; } = null!;

    public string MediaUrl { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;
}
