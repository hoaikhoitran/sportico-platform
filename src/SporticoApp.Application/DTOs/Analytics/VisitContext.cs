namespace SporticoApp.Application.DTOs.Analytics
{
    /// <summary>
    /// Session-identity signals shared by both tracked activities (backend API request, frontend
    /// page view). Deliberately does NOT carry a path/method — those are activity-specific, see
    /// <see cref="VisitorTrackingWorkItem"/>.
    /// </summary>
    public class VisitContext
    {
        public Guid VisitorId { get; set; }

        /// <summary>Raw client IP. Hashed inside the tracking service; never persisted as-is.</summary>
        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        /// <summary>Best-effort, from a reverse-proxy geo header (e.g. CF-IPCountry). Null if unavailable.</summary>
        public string? Country { get; set; }

        /// <summary>Set when the request is authenticated.</summary>
        public Guid? UserId { get; set; }
    }
}
