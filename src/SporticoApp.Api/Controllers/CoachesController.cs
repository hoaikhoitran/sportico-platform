using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoachesController : ControllerBase
    {
        private readonly ICoachService _coachService;

        public CoachesController(ICoachService coachService)
        {
            _coachService = coachService;
        }

        [HttpPost("register")]
        [Authorize]
        [ProducesResponseType(typeof(Result<CoachProfileResponse>), 200)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterCoachRequest request)
        {
            var userId = User.GetUserId();

            var result =
                await _coachService.RegisterCoachAsync(userId, request);

            return Ok(result);
        }
    }
}