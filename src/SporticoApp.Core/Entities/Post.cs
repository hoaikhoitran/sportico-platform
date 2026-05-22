using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Bài đăng dịch vụ huấn luyện
/// </summary>
public partial class Post
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    public int SportId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? Location { get; set; }

    public bool IsOnline { get; set; }

    /// <summary>
    /// draft | published | archived | rejected
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Sport Sport { get; set; } = null!;
}
