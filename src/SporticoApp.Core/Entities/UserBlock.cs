using System;

namespace SporticoApp.Core.Entities;

/// <summary>One user blocking another. Composite key (BlockerId, BlockedUserId). Blocking is one-directional.</summary>
public partial class UserBlock
{
    public Guid BlockerId { get; set; }

    public Guid BlockedUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Reason { get; set; }

    public virtual User Blocker { get; set; } = null!;

    public virtual User BlockedUser { get; set; } = null!;
}
