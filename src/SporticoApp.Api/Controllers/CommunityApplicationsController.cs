using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/community/applications")]
    [Authorize]
    public class CommunityApplicationsController : ControllerBase
    {
        private readonly ICommunityPostService _postService;

        public CommunityApplicationsController(ICommunityPostService postService)
        {
            _postService = postService;
        }

        [HttpPut("{id:guid}/accept")]
        [ProducesResponseType(typeof(Result<CommunityApplicationResponse>), 200)]
        public async Task<IActionResult> Accept(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _postService.AcceptApplicationAsync(userId, id);
            return Ok(result);
        }

        [HttpPut("{id:guid}/reject")]
        [ProducesResponseType(typeof(Result<CommunityApplicationResponse>), 200)]
        public async Task<IActionResult> Reject(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _postService.RejectApplicationAsync(userId, id);
            return Ok(result);
        }
    }
}
