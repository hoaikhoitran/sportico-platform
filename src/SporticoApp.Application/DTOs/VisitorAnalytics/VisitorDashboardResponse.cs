namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    /// <summary>
    /// Single composite payload for GET /api/admin/analytics/dashboard. Every section reuses the
    /// same repository queries backing the dedicated single-purpose endpoints.
    /// </summary>
    public class VisitorDashboardResponse
    {
        public VisitorStatsResponse VisitorStats { get; set; } = new();

        public PageViewStatsResponse PageViewStats { get; set; } = new();

        public List<VisitorsChartPoint> VisitorsChart { get; set; } = new();

        public List<TopPageItem> TopPages { get; set; } = new();

        public List<DeviceBreakdownItem> Devices { get; set; } = new();

        public List<BrowserBreakdownItem> Browsers { get; set; } = new();

        public List<CountryBreakdownItem> Countries { get; set; } = new();
    }
}
