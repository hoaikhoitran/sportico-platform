using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.TrainingPlans;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class TrainingPlansController : ControllerBase
    {
        private readonly ITrainingPlanService _trainingPlanService;

        public TrainingPlansController(ITrainingPlanService trainingPlanService)
        {
            _trainingPlanService = trainingPlanService;
        }

        [HttpPost("api/bookings/{bookingId:guid}/training-plan")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<TrainingPlanResponse>), 200)]
        public async Task<IActionResult> Create(
            Guid bookingId,
            [FromBody] CreateTrainingPlanRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPlanService.CreateAsync(coachId, bookingId, request);
            return Ok(result);
        }

        [HttpGet("api/bookings/{bookingId:guid}/training-plan")]
        [Authorize]
        [ProducesResponseType(typeof(Result<TrainingPlanResponse>), 200)]
        public async Task<IActionResult> GetByBooking(Guid bookingId)
        {
            var userId = User.GetUserId();
            var result = await _trainingPlanService.GetByBookingAsync(userId, bookingId);
            return Ok(result);
        }

        [HttpPut("api/training-plans/{id:guid}")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<TrainingPlanResponse>), 200)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateTrainingPlanRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPlanService.UpdateAsync(coachId, id, request);
            return Ok(result);
        }

        [HttpPost("api/training-plans/{id:guid}/weeks")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<TrainingPlanWeekResponse>), 200)]
        public async Task<IActionResult> AddWeek(
            Guid id,
            [FromBody] CreateTrainingPlanWeekRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPlanService.AddWeekAsync(coachId, id, request);
            return Ok(result);
        }

        [HttpPost("api/training-plan-weeks/{weekId:guid}/days")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<TrainingPlanDayResponse>), 200)]
        public async Task<IActionResult> AddDay(
            Guid weekId,
            [FromBody] CreateTrainingPlanDayRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPlanService.AddDayAsync(coachId, weekId, request);
            return Ok(result);
        }

        [HttpPost("api/training-plan-days/{dayId:guid}/exercises")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<TrainingPlanExerciseResponse>), 200)]
        public async Task<IActionResult> AddExercise(
            Guid dayId,
            [FromBody] CreateTrainingPlanExerciseRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPlanService.AddExerciseAsync(coachId, dayId, request);
            return Ok(result);
        }

        [HttpPut("api/training-plan-exercises/{id:guid}")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<TrainingPlanExerciseResponse>), 200)]
        public async Task<IActionResult> UpdateExercise(
            Guid id,
            [FromBody] UpdateTrainingPlanExerciseRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPlanService.UpdateExerciseAsync(coachId, id, request);
            return Ok(result);
        }

        [HttpDelete("api/training-plan-exercises/{id:guid}")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> DeleteExercise(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPlanService.DeleteExerciseAsync(coachId, id);
            return Ok(result);
        }
    }
}
