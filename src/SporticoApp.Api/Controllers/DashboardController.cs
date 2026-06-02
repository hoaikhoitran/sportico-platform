using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Dashboard;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("api/coaches/me/dashboard")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<CoachDashboardResponse>), 200)]
        public async Task<IActionResult> GetCoachDashboard([FromQuery] DashboardFilterRequest filter)
        {
            var coachId = User.GetUserId();
            var result = await _dashboardService.GetCoachDashboardAsync(coachId, filter);
            return Ok(result);
        }

        [HttpGet("api/admin/dashboard")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<AdminDashboardResponse>), 200)]
        public async Task<IActionResult> GetAdminDashboard([FromQuery] DashboardFilterRequest filter)
        {
            var result = await _dashboardService.GetAdminDashboardAsync(filter);
            return Ok(result);
        }
    }
}
