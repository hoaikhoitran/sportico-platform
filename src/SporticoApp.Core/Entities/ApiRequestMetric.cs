using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// One tracked BACKEND API request within a <see cref="VisitorSession"/> (method/path/status of a
/// call to this ASP.NET Core API). This is backend API usage telemetry — it is NOT a frontend page
/// view. See <see cref="PageView"/> for actual frontend navigation events, which the frontend SPA
/// submits explicitly because the backend cannot observe client-side route changes.
/// </summary>
public partial class ApiRequestMetric
{
    public Guid Id { get; set; }

    public Guid VisitorSessionId { get; set; }

    /// <summary>Denormalized from the session for cheap per-user API-usage queries.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Backend route, e.g. "/api/bookings/purchase/payos".</summary>
    public string Path { get; set; } = null!;

    public string Method { get; set; } = null!;

    public int? StatusCode { get; set; }

    public DateTime RequestedAt { get; set; }

    public virtual VisitorSession VisitorSession { get; set; } = null!;

    public virtual User? User { get; set; }
}
