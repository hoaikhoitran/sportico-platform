using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    /// <summary>Report a community post, community comment, or chat message (shared Report table).</summary>
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ICommunityReportService _reportService;

        public ReportsController(ICommunityReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(Result<ReportResponse>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateReportRequest request)
        {
            var userId = User.GetUserId();
            var result = await _reportService.CreateAsync(userId, request);
            return Ok(result);
        }
    }
}
