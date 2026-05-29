using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/coaches/me/teaching-locations")]
    [Authorize(Roles = RoleConstants.Coach)]
    public class CoachTeachingLocationsController : ControllerBase
    {
        private readonly ICoachTeachingLocationService _locationService;

        public CoachTeachingLocationsController(ICoachTeachingLocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<List<CoachTeachingLocationResponse>>), 200)]
        public async Task<IActionResult> GetMyLocations()
        {
            var coachId = User.GetUserId();
            var result = await _locationService.GetMyLocationsAsync(coachId);
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(Result<CoachTeachingLocationResponse>), 200)]
        public async Task<IActionResult> Create(
            [FromBody] CreateCoachTeachingLocationRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _locationService.CreateAsync(coachId, request);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<CoachTeachingLocationResponse>), 200)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateCoachTeachingLocationRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _locationService.UpdateAsync(coachId, id, request);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _locationService.DeleteAsync(coachId, id);
            return Ok(result);
        }

        [HttpPut("{id:guid}/set-default")]
        [ProducesResponseType(typeof(Result<CoachTeachingLocationResponse>), 200)]
        public async Task<IActionResult> SetDefault(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _locationService.SetDefaultAsync(coachId, id);
            return Ok(result);
        }
    }
}
