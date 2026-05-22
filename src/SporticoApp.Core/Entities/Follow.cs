using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// User theo dõi User khác (chủ yếu learner follow Coach)
/// </summary>
public partial class Follow
{
    public Guid FollowerId { get; set; }

    public Guid FollowingId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User follower { get; set; } = null!;

    public virtual User following { get; set; } = null!;
}
