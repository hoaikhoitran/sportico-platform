using SporticoApp.Application.DTOs.VisitorAnalytics;

namespace SporticoApp.Application.Interfaces.Repositories
{
    /// <summary>Read-only aggregate queries backing the admin visitor-analytics dashboard.</summary>
    public interface IVisitorAnalyticsRepository
    {
        Task<VisitorStatsResponse> GetVisitorStatsAsync(DateTime? fromDate, DateTime? toDate);

        Task<PageViewStatsResponse> GetPageViewStatsAsync(DateTime? fromDate, DateTime? toDate);

        Task<List<TopPageItem>> GetTopPagesAsync(DateTime? fromDate, DateTime? toDate, int limit);

        Task<List<DeviceBreakdownItem>> GetDeviceBreakdownAsync(DateTime? fromDate, DateTime? toDate);

        Task<List<BrowserBreakdownItem>> GetBrowserBreakdownAsync(DateTime? fromDate, DateTime? toDate);

        Task<List<CountryBreakdownItem>> GetCountryBreakdownAsync(DateTime? fromDate, DateTime? toDate);

        Task<List<VisitorsChartPoint>> GetVisitorsChartAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string granularity);
    }
}
