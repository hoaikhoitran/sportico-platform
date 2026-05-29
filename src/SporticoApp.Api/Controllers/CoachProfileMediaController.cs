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
    [Route("api/coaches/me/media")]
    [Authorize(Roles = RoleConstants.Coach)]
    public class CoachProfileMediaController : ControllerBase
    {
        private readonly ICoachProfileMediaService _mediaService;

        public CoachProfileMediaController(ICoachProfileMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<List<CoachProfileMediaResponse>>), 200)]
        public async Task<IActionResult> GetMyMedia()
        {
            var coachId = User.GetUserId();
            var result = await _mediaService.GetMyMediaAsync(coachId);
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(Result<CoachProfileMediaResponse>), 200)]
        public async Task<IActionResult> Create(
            [FromBody] CreateCoachProfileMediaRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _mediaService.CreateAsync(coachId, request);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<CoachProfileMediaResponse>), 200)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateCoachProfileMediaRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _mediaService.UpdateAsync(coachId, id, request);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _mediaService.DeleteAsync(coachId, id);
            return Ok(result);
        }
    }
}
