using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Phòng chat 1-1 giữa 2 User
/// </summary>
public partial class ChatRoom
{
    public Guid Id { get; set; }

    public Guid User1Id { get; set; }

    public Guid User2Id { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>pending | active | rejected. Pre-existing rooms are backfilled to "active".</summary>
    public string Status { get; set; } = "active";

    /// <summary>Who opened the room — the "requester" whose first message put it into pending.</summary>
    public Guid? RequestedByUserId { get; set; }

    public DateTime? RequestedAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    /// <summary>Context the room was opened from, e.g. "community_post". Null for direct/coach chats.</summary>
    public string? SourceType { get; set; }

    public Guid? SourceId { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual User User1 { get; set; } = null!;

    public virtual User User2 { get; set; } = null!;
}
