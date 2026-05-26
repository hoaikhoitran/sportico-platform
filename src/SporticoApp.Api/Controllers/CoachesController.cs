using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
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
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdValue) ||
                !Guid.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedException(
                    ErrorCodes.InvalidCredentials,
                    "Invalid access token");
            }

            var result = await _coachService.RegisterCoachAsync(userId, request);
            return Ok(result);
        }
    }
}
