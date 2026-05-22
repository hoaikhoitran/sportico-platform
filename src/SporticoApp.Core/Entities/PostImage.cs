using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Hình ảnh kèm bài đăng
/// </summary>
public partial class PostImage
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Post Post { get; set; } = null!;
}
