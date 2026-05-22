using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Bảng người dùng cốt lõi
/// </summary>
public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>
    /// active | inactive | banned | pending
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ChatRoom> ChatRoomsAsUser1 { get; set; } = new List<ChatRoom>();

    public virtual ICollection<ChatRoom> ChatRoomsAsUser2 { get; set; } = new List<ChatRoom>();

    public virtual CoachProfile? CoachProfile { get; set; }

    public virtual ICollection<Follow> FollowsAsFollower { get; set; } = new List<Follow>();

    public virtual ICollection<Follow> FollowsAsFollowing { get; set; } = new List<Follow>();

    public virtual LearnerProfile? LearnerProfile { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Report> ReportsAsReporter { get; set; } = new List<Report>();

    public virtual ICollection<Report> ReportsAsTargetUser { get; set; } = new List<Report>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
