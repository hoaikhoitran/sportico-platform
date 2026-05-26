using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Posts;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/posts")]
    [Authorize(Roles = RoleConstants.Coach)]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostsController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(Result<PostResponse>), 200)]
        public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _postService.CreateAsync(coachId, request);
            return Ok(result);
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(Result<PagedResult<PostResponse>>), 200)]
        public async Task<IActionResult> GetMyPosts([FromQuery] PostFilterRequest filter)
        {
            var coachId = User.GetUserId();
            var result = await _postService.GetMyPostsAsync(coachId, filter);
            return Ok(result);
        }

        [HttpGet("me/{id:guid}")]
        [ProducesResponseType(typeof(Result<PostResponse>), 200)]
        public async Task<IActionResult> GetMyPostById(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _postService.GetMyPostByIdAsync(coachId, id);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<PostResponse>), 200)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdatePostRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _postService.UpdateAsync(coachId, id, request);
            return Ok(result);
        }

        [HttpPut("{id:guid}/archive")]
        [ProducesResponseType(typeof(Result<PostResponse>), 200)]
        public async Task<IActionResult> Archive(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _postService.ArchiveAsync(coachId, id);
            return Ok(result);
        }
    }
}
