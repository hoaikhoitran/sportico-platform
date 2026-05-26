using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.Posts;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin/posts")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminPostsController : ControllerBase
    {
        private readonly IAdminPostService _adminPostService;

        public AdminPostsController(IAdminPostService adminPostService)
        {
            _adminPostService = adminPostService;
        }

        [HttpGet("pending")]
        [ProducesResponseType(typeof(Result<PagedResult<PostResponse>>), 200)]
        public async Task<IActionResult> GetPending([FromQuery] PostFilterRequest filter)
        {
            var result = await _adminPostService.GetPendingPostsAsync(filter);
            return Ok(result);
        }

        [HttpPut("{id:guid}/approve")]
        [ProducesResponseType(typeof(Result<PostResponse>), 200)]
        public async Task<IActionResult> Approve(Guid id)
        {
            var result = await _adminPostService.ApproveAsync(id);
            return Ok(result);
        }

        [HttpPut("{id:guid}/reject")]
        [ProducesResponseType(typeof(Result<PostResponse>), 200)]
        public async Task<IActionResult> Reject(
            Guid id,
            [FromBody] RejectPostRequest request)
        {
            var result = await _adminPostService.RejectAsync(id, request);
            return Ok(result);
        }
    }
}
