using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin/community")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminCommunityController : ControllerBase
    {
        private readonly IAdminCommunityService _adminCommunityService;

        public AdminCommunityController(IAdminCommunityService adminCommunityService)
        {
            _adminCommunityService = adminCommunityService;
        }

        [HttpGet("posts")]
        [ProducesResponseType(typeof(Result<PagedResult<AdminCommunityPostResponse>>), 200)]
        public async Task<IActionResult> GetPosts([FromQuery] AdminCommunityPostFilterRequest filter)
        {
            var result = await _adminCommunityService.GetPostsAsync(filter);
            return Ok(result);
        }

        [HttpGet("posts/{id:guid}")]
        [ProducesResponseType(typeof(Result<CommunityPostResponse>), 200)]
        public async Task<IActionResult> GetPostById(Guid id)
        {
            var result = await _adminCommunityService.GetPostByIdAsync(id);
            return Ok(result);
        }

        [HttpPut("posts/{id:guid}/hide")]
        [ProducesResponseType(typeof(Result<CommunityPostResponse>), 200)]
        public async Task<IActionResult> HidePost(Guid id, [FromBody] HideContentRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _adminCommunityService.HidePostAsync(adminId, id, request);
            return Ok(result);
        }

        [HttpPut("posts/{id:guid}/restore")]
        [ProducesResponseType(typeof(Result<CommunityPostResponse>), 200)]
        public async Task<IActionResult> RestorePost(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _adminCommunityService.RestorePostAsync(adminId, id);
            return Ok(result);
        }

        [HttpDelete("posts/{id:guid}")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _adminCommunityService.DeletePostAsync(adminId, id);
            return Ok(result);
        }

        [HttpGet("posts/{id:guid}/comments")]
        [ProducesResponseType(typeof(Result<PagedResult<CommunityCommentResponse>>), 200)]
        public async Task<IActionResult> GetComments(Guid id, [FromQuery] CommunityCommentFilterRequest filter)
        {
            var result = await _adminCommunityService.GetCommentsAsync(id, filter);
            return Ok(result);
        }

        [HttpPut("comments/{id:guid}/hide")]
        [ProducesResponseType(typeof(Result<CommunityCommentResponse>), 200)]
        public async Task<IActionResult> HideComment(Guid id, [FromBody] HideContentRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _adminCommunityService.HideCommentAsync(adminId, id, request);
            return Ok(result);
        }

        [HttpPut("comments/{id:guid}/restore")]
        [ProducesResponseType(typeof(Result<CommunityCommentResponse>), 200)]
        public async Task<IActionResult> RestoreComment(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _adminCommunityService.RestoreCommentAsync(adminId, id);
            return Ok(result);
        }

        [HttpDelete("comments/{id:guid}")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _adminCommunityService.DeleteCommentAsync(adminId, id);
            return Ok(result);
        }

        [HttpGet("reports")]
        [ProducesResponseType(typeof(Result<PagedResult<ReportResponse>>), 200)]
        public async Task<IActionResult> GetReports([FromQuery] AdminReportFilterRequest filter)
        {
            var result = await _adminCommunityService.GetReportsAsync(filter);
            return Ok(result);
        }

        [HttpPut("reports/{id:guid}/resolve")]
        [ProducesResponseType(typeof(Result<ReportResponse>), 200)]
        public async Task<IActionResult> ResolveReport(Guid id, [FromBody] ResolveReportRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _adminCommunityService.ResolveReportAsync(adminId, id, request);
            return Ok(result);
        }
    }
}
