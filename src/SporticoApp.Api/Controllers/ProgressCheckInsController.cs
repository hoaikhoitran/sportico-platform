using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.ProgressCheckIns;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class ProgressCheckInsController : ControllerBase
    {
        private readonly IProgressCheckInService _progressCheckInService;

        public ProgressCheckInsController(IProgressCheckInService progressCheckInService)
        {
            _progressCheckInService = progressCheckInService;
        }

        [HttpPost("api/bookings/{bookingId:guid}/progress-checkins")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<ProgressCheckInResponse>), 200)]
        public async Task<IActionResult> Create(
            Guid bookingId,
            [FromBody] CreateProgressCheckInRequest request)
        {
            var learnerId = User.GetUserId();
            var result = await _progressCheckInService.CreateAsync(learnerId, bookingId, request);
            return Ok(result);
        }

        [HttpGet("api/bookings/{bookingId:guid}/progress-checkins")]
        [Authorize]
        [ProducesResponseType(typeof(Result<PagedResult<ProgressCheckInResponse>>), 200)]
        public async Task<IActionResult> GetByBooking(
            Guid bookingId,
            [FromQuery] ProgressCheckInFilterRequest filter)
        {
            var userId = User.GetUserId();
            var result = await _progressCheckInService.GetByBookingAsync(userId, bookingId, filter);
            return Ok(result);
        }

        [HttpPut("api/progress-checkins/{id:guid}/coach-feedback")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<ProgressCheckInResponse>), 200)]
        public async Task<IActionResult> UpdateFeedback(
            Guid id,
            [FromBody] UpdateProgressCheckInFeedbackRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _progressCheckInService.UpdateFeedbackAsync(coachId, id, request);
            return Ok(result);
        }
    }
}
