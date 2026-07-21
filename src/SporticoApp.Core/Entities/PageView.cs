using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// One FRONTEND navigation event within a <see cref="VisitorSession"/>. Populated only via the
/// frontend explicitly submitting the route it navigated to (<c>POST /api/analytics/pageview</c>)
/// — the backend never infers/fakes a frontend route from the backend API path it happened to
/// receive. See <see cref="ApiRequestMetric"/> for backend API usage telemetry.
/// </summary>
public partial class PageView
{
    public Guid Id { get; set; }

    public Guid VisitorSessionId { get; set; }

    /// <summary>Denormalized from the session for cheap per-user page-view queries.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Frontend route as reported by the client, e.g. "/coaches/123".</summary>
    public string Path { get; set; } = null!;

    public string? Title { get; set; }

    public string? Referrer { get; set; }

    public DateTime ViewedAt { get; set; }

    public virtual VisitorSession VisitorSession { get; set; } = null!;

    public virtual User? User { get; set; }
}
