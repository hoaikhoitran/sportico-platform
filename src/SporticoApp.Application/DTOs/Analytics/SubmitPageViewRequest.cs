namespace SporticoApp.Application.DTOs.Analytics
{
    /// <summary>
    /// Body of POST /api/analytics/pageview. The frontend SPA calls this on every client-side route
    /// change. Path is the ACTUAL frontend route as the client sees it — the backend never infers or
    /// fakes a frontend route from the backend API path a request happened to hit.
    /// </summary>
    public class SubmitPageViewRequest
    {
        public string Path { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string? Referrer { get; set; }
    }
}
