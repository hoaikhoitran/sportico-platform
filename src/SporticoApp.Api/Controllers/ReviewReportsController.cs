using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class ReviewReportsController : ControllerBase
    {
        private readonly IReviewReportService _reviewReportService;

        public ReviewReportsController(IReviewReportService reviewReportService)
        {
            _reviewReportService = reviewReportService;
        }

        // ── Coach ─────────────────────────────────────────────────────────────

        [HttpPost("api/reviews/{id:guid}/report")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<ReviewReportResponse>), 200)]
        public async Task<IActionResult> ReportReview(
            Guid id,
            [FromBody] CreateReviewReportRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _reviewReportService.ReportAsync(coachId, id, request);
            return Ok(result);
        }

        // ── Admin ─────────────────────────────────────────────────────────────

        [HttpGet("api/admin/review-reports")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<PagedResult<ReviewReportResponse>>), 200)]
        public async Task<IActionResult> GetReports([FromQuery] ReviewReportFilterRequest filter)
        {
            var result = await _reviewReportService.GetReportsAsync(filter);
            return Ok(result);
        }

        [HttpPut("api/admin/review-reports/{id:guid}/resolve")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<ReviewReportResponse>), 200)]
        public async Task<IActionResult> ResolveReport(
            Guid id,
            [FromBody] ResolveReviewReportRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _reviewReportService.ResolveAsync(adminId, id, request);
            return Ok(result);
        }
    }
}
