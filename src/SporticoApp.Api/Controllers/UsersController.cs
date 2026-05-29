using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public UsersController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(Result<CurrentUserResponse>), 200)]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.GetUserId();
            var result = await _userProfileService.GetMeAsync(userId);
            return Ok(result);
        }

        [HttpPut("me")]
        [ProducesResponseType(typeof(Result<CurrentUserResponse>), 200)]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request)
        {
            var userId = User.GetUserId();
            var result = await _userProfileService.UpdateMeAsync(userId, request);
            return Ok(result);
        }
    }
}
