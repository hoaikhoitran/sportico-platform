using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/community/posts")]
    [AllowAnonymous]
    public class CommunityPostsController : ControllerBase
    {
        private readonly ICommunityPostService _postService;

        public CommunityPostsController(ICommunityPostService postService)
        {
            _postService = postService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<CommunityPostResponse>>), 200)]
        public async Task<IActionResult> GetFeed([FromQuery] CommunityPostFilterRequest filter)
        {
            var currentUserId = User.GetUserIdOrNull();
            var result = await _postService.GetFeedAsync(currentUserId, filter);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(Result<PagedResult<CommunityPostResponse>>), 200)]
        public async Task<IActionResult> GetMyPosts([FromQuery] CommunityPostFilterRequest filter)
        {
            var userId = User.GetUserId();
            var result = await _postService.GetMyPostsAsync(userId, filter);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<CommunityPostResponse>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = User.GetUserIdOrNull();
            var result = await _postService.GetByIdAsync(currentUserId, id);
            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(Result<CommunityPostResponse>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateCommunityPostRequest request)
        {
            var userId = User.GetUserId();
            var result = await _postService.CreateAsync(userId, request);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Result<CommunityPostResponse>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCommunityPostRequest request)
        {
            var userId = User.GetUserId();
            var result = await _postService.UpdateAsync(userId, id, request);
            return Ok(result);
        }

        [HttpPut("{id:guid}/close")]
        [Authorize]
        [ProducesResponseType(typeof(Result<CommunityPostResponse>), 200)]
        public async Task<IActionResult> Close(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _postService.CloseAsync(userId, id);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _postService.DeleteAsync(userId, id);
            return Ok(result);
        }

        [HttpPut("{id:guid}/like")]
        [Authorize]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> Like(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _postService.LikeAsync(userId, id);
            return Ok(result);
        }

        [HttpDelete("{id:guid}/like")]
        [Authorize]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> Unlike(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _postService.UnlikeAsync(userId, id);
            return Ok(result);
        }

        [HttpPost("{id:guid}/applications")]
        [Authorize]
        [ProducesResponseType(typeof(Result<CommunityApplicationResponse>), 200)]
        public async Task<IActionResult> Apply(Guid id, [FromBody] CreateApplicationRequest request)
        {
            var userId = User.GetUserId();
            var result = await _postService.ApplyAsync(userId, id, request);
            return Ok(result);
        }

        [HttpDelete("{id:guid}/applications/me")]
        [Authorize]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> CancelMyApplication(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _postService.CancelMyApplicationAsync(userId, id);
            return Ok(result);
        }

        [HttpGet("{id:guid}/applications")]
        [Authorize]
        [ProducesResponseType(typeof(Result<PagedResult<CommunityApplicationResponse>>), 200)]
        public async Task<IActionResult> GetApplications(Guid id, [FromQuery] CommunityApplicationFilterRequest filter)
        {
            var userId = User.GetUserId();
            var result = await _postService.GetApplicationsAsync(userId, id, filter);
            return Ok(result);
        }
    }
}
