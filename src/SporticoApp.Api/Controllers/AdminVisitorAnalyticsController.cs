using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.VisitorAnalytics;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    /// <summary>Admin website-visitor analytics. Read-only — reuses IVisitorAnalyticsService for all logic.</summary>
    [ApiController]
    [Route("api/admin/analytics")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminVisitorAnalyticsController : ControllerBase
    {
        private readonly IVisitorAnalyticsService _visitorAnalyticsService;

        public AdminVisitorAnalyticsController(IVisitorAnalyticsService visitorAnalyticsService)
        {
            _visitorAnalyticsService = visitorAnalyticsService;
        }

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(Result<VisitorDashboardResponse>), 200)]
        public async Task<IActionResult> GetDashboard([FromQuery] VisitorAnalyticsFilterRequest filter)
        {
            var result = await _visitorAnalyticsService.GetDashboardAsync(filter);
            return Ok(result);
        }

        [HttpGet("visitors")]
        [ProducesResponseType(typeof(Result<VisitorStatsResponse>), 200)]
        public async Task<IActionResult> GetVisitors([FromQuery] VisitorAnalyticsFilterRequest filter)
        {
            var result = await _visitorAnalyticsService.GetVisitorStatsAsync(filter);
            return Ok(result);
        }

        [HttpGet("pageviews")]
        [ProducesResponseType(typeof(Result<PageViewStatsResponse>), 200)]
        public async Task<IActionResult> GetPageViews([FromQuery] VisitorAnalyticsFilterRequest filter)
        {
            var result = await _visitorAnalyticsService.GetPageViewStatsAsync(filter);
            return Ok(result);
        }

        [HttpGet("top-pages")]
        [ProducesResponseType(typeof(Result<List<TopPageItem>>), 200)]
        public async Task<IActionResult> GetTopPages([FromQuery] TopPagesFilterRequest filter)
        {
            var result = await _visitorAnalyticsService.GetTopPagesAsync(filter);
            return Ok(result);
        }

        [HttpGet("devices")]
        [ProducesResponseType(typeof(Result<List<DeviceBreakdownItem>>), 200)]
        public async Task<IActionResult> GetDevices([FromQuery] VisitorAnalyticsFilterRequest filter)
        {
            var result = await _visitorAnalyticsService.GetDeviceBreakdownAsync(filter);
            return Ok(result);
        }

        [HttpGet("browsers")]
        [ProducesResponseType(typeof(Result<List<BrowserBreakdownItem>>), 200)]
        public async Task<IActionResult> GetBrowsers([FromQuery] VisitorAnalyticsFilterRequest filter)
        {
            var result = await _visitorAnalyticsService.GetBrowserBreakdownAsync(filter);
            return Ok(result);
        }

        [HttpGet("countries")]
        [ProducesResponseType(typeof(Result<List<CountryBreakdownItem>>), 200)]
        public async Task<IActionResult> GetCountries([FromQuery] VisitorAnalyticsFilterRequest filter)
        {
            var result = await _visitorAnalyticsService.GetCountryBreakdownAsync(filter);
            return Ok(result);
        }

        [HttpGet("chart")]
        [ProducesResponseType(typeof(Result<List<VisitorsChartPoint>>), 200)]
        public async Task<IActionResult> GetVisitorsChart([FromQuery] VisitorsChartFilterRequest filter)
        {
            var result = await _visitorAnalyticsService.GetVisitorsChartAsync(filter);
            return Ok(result);
        }
    }
}
