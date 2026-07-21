namespace SporticoApp.Application.DTOs.Analytics
{
    public enum VisitorTrackingWorkItemKind
    {
        /// <summary>Backend API call → persisted as an ApiRequestMetric.</summary>
        ApiRequest,

        /// <summary>Frontend navigation event submitted by the client → persisted as a PageView.</summary>
        PageView
    }

    /// <summary>
    /// One unit of work for the background visitor-tracking consumer (see IVisitorTrackingQueue /
    /// VisitorTrackingBackgroundService). Built entirely from data already captured on the request
    /// thread — the consumer only ever does I/O, never touches HttpContext.
    /// </summary>
    public class VisitorTrackingWorkItem
    {
        public required VisitContext Context { get; init; }

        public required VisitorTrackingWorkItemKind Kind { get; init; }

        /// <summary>Backend route (ApiRequest) or frontend route (PageView) depending on Kind.</summary>
        public string Path { get; init; } = string.Empty;

        /// <summary>ApiRequest only.</summary>
        public string? Method { get; init; }

        /// <summary>ApiRequest only.</summary>
        public int? StatusCode { get; init; }

        /// <summary>PageView only.</summary>
        public string? Title { get; init; }

        /// <summary>PageView only.</summary>
        public string? Referrer { get; init; }
    }
}
