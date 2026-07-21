using SporticoApp.Application.DTOs.Analytics;

namespace SporticoApp.Application.Interfaces.Services
{
    /// <summary>
    /// Write-side of visitor analytics. Called only from the background consumer
    /// (VisitorTrackingBackgroundService), never inline on the HTTP request thread.
    /// </summary>
    public interface IVisitorTrackingService
    {
        /// <summary>Records a backend API call (ApiRequestMetric) and touches/creates the session.</summary>
        Task TrackApiRequestAsync(VisitContext context, string path, string method, int statusCode);

        /// <summary>Records a frontend navigation event (PageView) and touches/creates the session.</summary>
        Task TrackPageViewAsync(VisitContext context, string path, string? title, string? referrer);
    }
}
