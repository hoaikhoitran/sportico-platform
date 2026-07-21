namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    public class PageViewStatsResponse
    {
        public int TotalPageViews { get; set; }

        public int TodayPageViews { get; set; }

        public decimal AveragePageViewsPerSession { get; set; }
    }
}
