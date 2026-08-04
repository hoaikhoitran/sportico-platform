using System;

namespace SporticoApp.Core.Entities;

/// <summary>Like on a <see cref="CommunityPost"/>. MVP: "like" is the only reaction type. One row per (post, user).</summary>
public partial class CommunityPostReaction
{
    public Guid PostId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CommunityPost Post { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
