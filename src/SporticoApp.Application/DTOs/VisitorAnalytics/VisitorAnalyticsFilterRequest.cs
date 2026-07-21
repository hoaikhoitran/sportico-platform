namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    /// <summary>Optional date range for visitor/pageview stats and breakdown endpoints.</summary>
    public class VisitorAnalyticsFilterRequest
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
