using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// An AI advisory chatbot conversation started by a learner or an admin.
/// Scoped to the initiating user via <see cref="UserId"/>.
/// </summary>
public partial class AdvisoryConversation
{
    public Guid Id { get; set; }

    /// <summary>The user (learner or admin) who started the conversation.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Role that initiated the conversation: learner | admin
    /// (see <see cref="SporticoApp.Shared.Constants.RoleConstants"/>).
    /// </summary>
    public string InitiatorRole { get; set; } = null!;

    /// <summary>Short title derived from the first user message; optional.</summary>
    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<AdvisoryMessage> Messages { get; set; } = new List<AdvisoryMessage>();
}
