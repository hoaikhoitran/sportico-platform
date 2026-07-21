using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// One browsing session by an anonymous or logged-in visitor, identified by a first-party cookie
/// (<see cref="VisitorId"/>). A session closes after a period of inactivity; the next visit after
/// that starts a new <see cref="VisitorSession"/> row for the same <see cref="VisitorId"/>.
/// </summary>
public partial class VisitorSession
{
    public Guid Id { get; set; }

    /// <summary>Stable anonymous identifier from a first-party cookie. Not tied to any account.</summary>
    public Guid VisitorId { get; set; }

    /// <summary>SHA-256 hash of the client IP (+ configured pepper). The raw IP is never stored.</summary>
    public string IpHash { get; set; } = null!;

    public string? UserAgent { get; set; }

    /// <summary>desktop | mobile | tablet | bot | unknown</summary>
    public string? Device { get; set; }

    public string? Browser { get; set; }

    public string? Os { get; set; }

    /// <summary>Best-effort, from a reverse-proxy geo header if present; null otherwise.</summary>
    public string? Country { get; set; }

    /// <summary>Set when the visitor was authenticated at any point during this session.</summary>
    public Guid? UserId { get; set; }

    /// <summary>True if this VisitorId had no prior session before this one (first-ever visit).</summary>
    public bool IsNewVisitor { get; set; }

    /// <summary>Count of frontend PageView events (navigation) in this session. NOT API calls.</summary>
    public int PageViewCount { get; set; }

    /// <summary>Count of backend ApiRequestMetric events (API calls) in this session. NOT page views.</summary>
    public int ApiRequestCount { get; set; }

    public DateTime FirstSeenAt { get; set; }

    /// <summary>
    /// Last activity of ANY kind (page view or API call) — touched on both, since either indicates
    /// the visitor is still active. Also the boundary used by the session-idle-timeout check.
    /// </summary>
    public DateTime LastSeenAt { get; set; }

    /// <summary>
    /// LastSeenAt − FirstSeenAt, refreshed on every touch within the session. See
    /// VisitorTrackingService for why idle gaps can never inflate this beyond the session-idle
    /// timeout (a gap that large would have started a new session instead).
    /// </summary>
    public int DurationSeconds { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }

    public virtual ICollection<PageView> PageViews { get; set; } = new List<PageView>();

    public virtual ICollection<ApiRequestMetric> ApiRequestMetrics { get; set; } = new List<ApiRequestMetric>();
}
