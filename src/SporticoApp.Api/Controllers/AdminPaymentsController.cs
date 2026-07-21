using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.AdminPayments;
using SporticoApp.Application.DTOs.Dashboard;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    /// <summary>Admin payment analytics/dashboard. Read-only — reuses IAdminPaymentService for all logic.</summary>
    [ApiController]
    [Route("api/admin/payments")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminPaymentsController : ControllerBase
    {
        private readonly IAdminPaymentService _adminPaymentService;

        public AdminPaymentsController(IAdminPaymentService adminPaymentService)
        {
            _adminPaymentService = adminPaymentService;
        }

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(Result<AdminPaymentDashboardResponse>), 200)]
        public async Task<IActionResult> GetDashboard([FromQuery] DashboardFilterRequest filter)
        {
            var result = await _adminPaymentService.GetDashboardAsync(filter);
            return Ok(result);
        }

        [HttpGet("statistics")]
        [ProducesResponseType(typeof(Result<PaymentStatisticsResponse>), 200)]
        public async Task<IActionResult> GetStatistics([FromQuery] DashboardFilterRequest filter)
        {
            var result = await _adminPaymentService.GetStatisticsAsync(filter);
            return Ok(result);
        }

        [HttpGet("revenue")]
        [ProducesResponseType(typeof(Result<RevenueSummaryResponse>), 200)]
        public async Task<IActionResult> GetRevenue([FromQuery] DashboardFilterRequest filter)
        {
            var result = await _adminPaymentService.GetRevenueAsync(filter);
            return Ok(result);
        }

        [HttpGet("chart")]
        [ProducesResponseType(typeof(Result<List<RevenueChartPoint>>), 200)]
        public async Task<IActionResult> GetRevenueChart([FromQuery] RevenueChartFilterRequest filter)
        {
            var result = await _adminPaymentService.GetRevenueChartAsync(filter);
            return Ok(result);
        }

        [HttpGet("top-coaches")]
        [ProducesResponseType(typeof(Result<List<TopCoachRevenueItem>>), 200)]
        public async Task<IActionResult> GetTopCoaches([FromQuery] TopEntitiesFilterRequest filter)
        {
            var result = await _adminPaymentService.GetTopCoachesAsync(filter);
            return Ok(result);
        }

        [HttpGet("top-sports")]
        [ProducesResponseType(typeof(Result<List<TopSportRevenueItem>>), 200)]
        public async Task<IActionResult> GetTopSports([FromQuery] TopEntitiesFilterRequest filter)
        {
            var result = await _adminPaymentService.GetTopSportsAsync(filter);
            return Ok(result);
        }

        [HttpGet("recent-transactions")]
        [ProducesResponseType(typeof(Result<List<AdminTransactionResponse>>), 200)]
        public async Task<IActionResult> GetRecentTransactions([FromQuery] int limit = 10)
        {
            var result = await _adminPaymentService.GetRecentTransactionsAsync(limit);
            return Ok(result);
        }

        [HttpGet("transactions")]
        [ProducesResponseType(typeof(Result<PagedResult<AdminTransactionResponse>>), 200)]
        public async Task<IActionResult> GetTransactions([FromQuery] AdminPaymentFilterRequest filter)
        {
            var result = await _adminPaymentService.GetTransactionsAsync(filter);
            return Ok(result);
        }
    }
}
