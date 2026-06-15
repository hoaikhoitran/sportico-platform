using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// A single turn within an <see cref="AdvisoryConversation"/>.
/// </summary>
public partial class AdvisoryMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    /// <summary>Who produced the message: user | assistant.</summary>
    public string Sender { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual AdvisoryConversation Conversation { get; set; } = null!;
}
