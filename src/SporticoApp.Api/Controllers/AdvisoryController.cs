using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Advisory;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/v1/advisory")]
    [Authorize(Roles = RoleConstants.Learner + "," + RoleConstants.Admin)]
    public class AdvisoryController : ControllerBase
    {
        private readonly IAdvisoryService _advisoryService;

        public AdvisoryController(IAdvisoryService advisoryService)
        {
            _advisoryService = advisoryService;
        }

        [HttpPost("messages")]
        [ProducesResponseType(typeof(Result<AdvisoryReplyDto>), 200)]
        public async Task<IActionResult> SendMessage([FromBody] SendAdvisoryMessageRequest request)
        {
            var userId = User.GetUserId();

            // Both roles share this endpoint; record which one started the conversation.
            var initiatorRole = User.IsInRole(RoleConstants.Admin)
                ? RoleConstants.Admin
                : RoleConstants.Learner;

            var result = await _advisoryService.SendMessageAsync(userId, initiatorRole, request);
            return Ok(result);
        }
    }
}
