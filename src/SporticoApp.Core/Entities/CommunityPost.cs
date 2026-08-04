using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Community forum / player-recruitment post. Independent of the legacy <see cref="Post"/> module —
/// its own table, service, repository and controllers (see docs/community-api.md).
/// </summary>
public partial class CommunityPost
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }

    public int? SportId { get; set; }

    /// <summary>See <see cref="Shared.Constants.CommunityPostTypes"/>.</summary>
    public string PostType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? LocationName { get; set; }

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    /// <summary>Includes the author. Required for recruitment post types.</summary>
    public int? MaxParticipants { get; set; }

    /// <summary>Starts at 1 (the author) for recruitment post types.</summary>
    public int AcceptedParticipants { get; set; }

    public string? Level { get; set; }

    public decimal? FeePerPerson { get; set; }

    /// <summary>draft | published | closed | expired | hidden | deleted.</summary>
    public string Status { get; set; } = null!;

    public bool AllowComments { get; set; } = true;

    public int CommentCount { get; set; }

    public int ReactionCount { get; set; }

    public int ApplicationCount { get; set; }

    public int ViewCount { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency token — guards AcceptedParticipants against concurrent accepts.</summary>
    public int Version { get; set; }

    public Guid? HiddenByUserId { get; set; }

    public DateTime? HiddenAt { get; set; }

    public string? ModerationReason { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual Sport? Sport { get; set; }

    public virtual ICollection<CommunityPostMedia> Media { get; set; } = new List<CommunityPostMedia>();

    public virtual ICollection<CommunityComment> Comments { get; set; } = new List<CommunityComment>();

    public virtual ICollection<CommunityPostReaction> Reactions { get; set; } = new List<CommunityPostReaction>();

    public virtual ICollection<CommunityPostApplication> Applications { get; set; } = new List<CommunityPostApplication>();
}
