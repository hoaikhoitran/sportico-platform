using SporticoApp.Application.DTOs.VisitorAnalytics;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IVisitorAnalyticsService
    {
        Task<Result<VisitorDashboardResponse>> GetDashboardAsync(VisitorAnalyticsFilterRequest filter);

        Task<Result<VisitorStatsResponse>> GetVisitorStatsAsync(VisitorAnalyticsFilterRequest filter);

        Task<Result<PageViewStatsResponse>> GetPageViewStatsAsync(VisitorAnalyticsFilterRequest filter);

        Task<Result<List<TopPageItem>>> GetTopPagesAsync(TopPagesFilterRequest filter);

        Task<Result<List<DeviceBreakdownItem>>> GetDeviceBreakdownAsync(VisitorAnalyticsFilterRequest filter);

        Task<Result<List<BrowserBreakdownItem>>> GetBrowserBreakdownAsync(VisitorAnalyticsFilterRequest filter);

        Task<Result<List<CountryBreakdownItem>>> GetCountryBreakdownAsync(VisitorAnalyticsFilterRequest filter);

        Task<Result<List<VisitorsChartPoint>>> GetVisitorsChartAsync(VisitorsChartFilterRequest filter);
    }
}
