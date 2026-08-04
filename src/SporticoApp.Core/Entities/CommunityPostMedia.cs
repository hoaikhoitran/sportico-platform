using System;

namespace SporticoApp.Core.Entities;

/// <summary>Image/video attached to a <see cref="CommunityPost"/>. MVP cap: 8 media, max 1 video (validated in the service).</summary>
public partial class CommunityPostMedia
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    /// <summary>image | video.</summary>
    public string MediaType { get; set; } = null!;

    public string Url { get; set; } = null!;

    /// <summary>Storage-provider key/path backing the Url, when the storage abstraction issues one.</summary>
    public string? StorageKey { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? MimeType { get; set; }

    public long? FileSize { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public int? DurationSeconds { get; set; }

    public int OrderIndex { get; set; }

    /// <summary>active | removed.</summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual CommunityPost Post { get; set; } = null!;
}
