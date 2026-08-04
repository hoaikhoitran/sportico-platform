using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Tin nhắn trong phòng chat
/// </summary>
public partial class Message
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid SenderId { get; set; }

    /// <summary>May be empty when the message is attachment-only (validated: content OR attachment required).</summary>
    public string Content { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime SentAt { get; set; }

    public virtual ICollection<MessageAttachment> MessageAttachments { get; set; } = new List<MessageAttachment>();

    public virtual ChatRoom Room { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
