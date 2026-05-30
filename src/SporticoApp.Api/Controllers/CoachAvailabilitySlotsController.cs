using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Availability;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class CoachAvailabilitySlotsController : ControllerBase
    {
        private readonly ICoachAvailabilityService _availabilityService;

        public CoachAvailabilitySlotsController(ICoachAvailabilityService availabilityService)
        {
            _availabilityService = availabilityService;
        }

        [HttpPost("api/coaches/me/availability-slots")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<CoachAvailabilitySlotResponse>), 200)]
        public async Task<IActionResult> CreateSlot([FromBody] CreateCoachAvailabilitySlotRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _availabilityService.CreateSlotAsync(coachId, request);
            return Ok(result);
        }

        [HttpGet("api/coaches/me/availability-slots")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<PagedResult<CoachAvailabilitySlotResponse>>), 200)]
        public async Task<IActionResult> GetMySlots([FromQuery] CoachAvailabilitySlotFilterRequest filter)
        {
            var coachId = User.GetUserId();
            var result = await _availabilityService.GetMySlotsAsync(coachId, filter);
            return Ok(result);
        }

        [HttpGet("api/coaches/{coachId:guid}/availability-slots")]
        [ProducesResponseType(typeof(Result<PagedResult<CoachAvailabilitySlotResponse>>), 200)]
        public async Task<IActionResult> GetCoachPublicSlots(
            Guid coachId,
            [FromQuery] CoachAvailabilitySlotFilterRequest filter)
        {
            var result = await _availabilityService.GetCoachPublicSlotsAsync(coachId, filter);
            return Ok(result);
        }

        [HttpPut("api/coaches/me/availability-slots/{id:guid}/cancel")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<CoachAvailabilitySlotResponse>), 200)]
        public async Task<IActionResult> CancelSlot(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _availabilityService.CancelSlotAsync(coachId, id);
            return Ok(result);
        }
    }
}
