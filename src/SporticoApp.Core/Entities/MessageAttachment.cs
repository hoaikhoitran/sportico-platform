using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// File đính kèm tin nhắn (image, video, doc...)
/// </summary>
public partial class MessageAttachment
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string? FileType { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Message Message { get; set; } = null!;
}
