using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.TrainingSessions;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class TrainingSessionsController : ControllerBase
    {
        private readonly ITrainingSessionService _trainingSessionService;

        public TrainingSessionsController(ITrainingSessionService trainingSessionService)
        {
            _trainingSessionService = trainingSessionService;
        }

        [HttpPost("api/bookings/{bookingId:guid}/sessions")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<TrainingSessionResponse>), 200)]
        public async Task<IActionResult> Create(
            Guid bookingId,
            [FromBody] CreateTrainingSessionRequest request)
        {
            var learnerId = User.GetUserId();
            request.BookingId = bookingId;
            var result = await _trainingSessionService.CreateAsync(learnerId, request);
            return Ok(result);
        }

        [HttpGet("api/bookings/{bookingId:guid}/sessions")]
        [Authorize]
        [ProducesResponseType(typeof(Result<PagedResult<TrainingSessionResponse>>), 200)]
        public async Task<IActionResult> GetByBooking(
            Guid bookingId,
            [FromQuery] TrainingSessionFilterRequest filter)
        {
            var userId = User.GetUserId();
            var result = await _trainingSessionService.GetByBookingAsync(userId, bookingId, filter);
            return Ok(result);
        }

        [HttpPut("api/training-sessions/{id:guid}/confirm")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<TrainingSessionResponse>), 200)]
        public async Task<IActionResult> Confirm(
            Guid id,
            [FromBody] ConfirmTrainingSessionRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _trainingSessionService.ConfirmAsync(coachId, id, request);
            return Ok(result);
        }

        [HttpPut("api/training-sessions/{id:guid}/cancel")]
        [Authorize]
        [ProducesResponseType(typeof(Result<TrainingSessionResponse>), 200)]
        public async Task<IActionResult> Cancel(
            Guid id,
            [FromBody] CancelTrainingSessionRequest request)
        {
            var userId = User.GetUserId();
            var result = await _trainingSessionService.CancelAsync(userId, id, request);
            return Ok(result);
        }

        [HttpPut("api/training-sessions/{id:guid}/complete")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<TrainingSessionResponse>), 200)]
        public async Task<IActionResult> Complete(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _trainingSessionService.CompleteAsync(coachId, id);
            return Ok(result);
        }
    }
}
