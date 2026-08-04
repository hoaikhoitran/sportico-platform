using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/community")]
    [AllowAnonymous]
    public class CommunityCommentsController : ControllerBase
    {
        private readonly ICommunityCommentService _commentService;

        public CommunityCommentsController(ICommunityCommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet("posts/{postId:guid}/comments")]
        [ProducesResponseType(typeof(Result<PagedResult<CommunityCommentResponse>>), 200)]
        public async Task<IActionResult> GetComments(Guid postId, [FromQuery] CommunityCommentFilterRequest filter)
        {
            var currentUserId = User.GetUserIdOrNull();
            var result = await _commentService.GetCommentsAsync(currentUserId, postId, filter);
            return Ok(result);
        }

        [HttpPost("posts/{postId:guid}/comments")]
        [Authorize]
        [ProducesResponseType(typeof(Result<CommunityCommentResponse>), 200)]
        public async Task<IActionResult> AddComment(Guid postId, [FromBody] CreateCommentRequest request)
        {
            var userId = User.GetUserId();
            var result = await _commentService.AddCommentAsync(userId, postId, request);
            return Ok(result);
        }

        [HttpPost("comments/{commentId:guid}/replies")]
        [Authorize]
        [ProducesResponseType(typeof(Result<CommunityCommentResponse>), 200)]
        public async Task<IActionResult> AddReply(Guid commentId, [FromBody] CreateReplyRequest request)
        {
            var userId = User.GetUserId();
            var result = await _commentService.AddReplyAsync(userId, commentId, request);
            return Ok(result);
        }

        [HttpPut("comments/{commentId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Result<CommunityCommentResponse>), 200)]
        public async Task<IActionResult> UpdateComment(Guid commentId, [FromBody] UpdateCommentRequest request)
        {
            var userId = User.GetUserId();
            var result = await _commentService.UpdateCommentAsync(userId, commentId, request);
            return Ok(result);
        }

        [HttpDelete("comments/{commentId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = User.GetUserId();
            var result = await _commentService.DeleteCommentAsync(userId, commentId);
            return Ok(result);
        }
    }
}
