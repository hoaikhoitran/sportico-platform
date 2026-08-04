using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Comment (or one-level reply) on a <see cref="CommunityPost"/>. Only one level of nesting is
/// supported: a comment whose ParentCommentId is itself a reply is rejected by the validator —
/// replies always attach to the root comment.
/// </summary>
public partial class CommunityComment
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Guid AuthorId { get; set; }

    /// <summary>Null for a root comment; set (to a ROOT comment's Id) for a one-level reply.</summary>
    public Guid? ParentCommentId { get; set; }

    public string Content { get; set; } = null!;

    /// <summary>active | hidden | deleted.</summary>
    public string Status { get; set; } = null!;

    public int ReplyCount { get; set; }

    public int ReactionCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? HiddenAt { get; set; }

    public Guid? HiddenByUserId { get; set; }

    public string? ModerationReason { get; set; }

    public virtual CommunityPost Post { get; set; } = null!;

    public virtual User Author { get; set; } = null!;

    public virtual CommunityComment? ParentComment { get; set; }

    public virtual ICollection<CommunityComment> Replies { get; set; } = new List<CommunityComment>();
}
