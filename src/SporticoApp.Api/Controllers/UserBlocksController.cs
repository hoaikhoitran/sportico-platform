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
    public class UserBlocksController : ControllerBase
    {
        private readonly IUserBlockService _userBlockService;

        public UserBlocksController(IUserBlockService userBlockService)
        {
            _userBlockService = userBlockService;
        }

        [HttpPut("{userId:guid}/block")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> Block(Guid userId, [FromBody] BlockUserRequest? request)
        {
            var currentUserId = User.GetUserId();
            var result = await _userBlockService.BlockAsync(currentUserId, userId, request ?? new BlockUserRequest());
            return Ok(result);
        }

        [HttpDelete("{userId:guid}/block")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> Unblock(Guid userId)
        {
            var currentUserId = User.GetUserId();
            var result = await _userBlockService.UnblockAsync(currentUserId, userId);
            return Ok(result);
        }

        [HttpGet("me/blocked")]
        [ProducesResponseType(typeof(Result<List<BlockedUserResponse>>), 200)]
        public async Task<IActionResult> GetBlocked()
        {
            var currentUserId = User.GetUserId();
            var result = await _userBlockService.GetBlockedAsync(currentUserId);
            return Ok(result);
        }
    }
}
