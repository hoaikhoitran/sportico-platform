using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.PayoutAccounts;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin/coach-payout-accounts")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminCoachPayoutAccountsController : ControllerBase
    {
        private readonly ICoachPayoutAccountService _payoutAccountService;

        public AdminCoachPayoutAccountsController(ICoachPayoutAccountService payoutAccountService)
        {
            _payoutAccountService = payoutAccountService;
        }

        [HttpGet("pending")]
        [ProducesResponseType(typeof(Result<PagedResult<CoachPayoutAccountResponse>>), 200)]
        public async Task<IActionResult> GetPending([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _payoutAccountService.GetPendingAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpPut("{id:guid}/verify")]
        [ProducesResponseType(typeof(Result<CoachPayoutAccountResponse>), 200)]
        public async Task<IActionResult> Verify(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _payoutAccountService.VerifyAsync(adminId, id);
            return Ok(result);
        }

        [HttpPut("{id:guid}/reject")]
        [ProducesResponseType(typeof(Result<CoachPayoutAccountResponse>), 200)]
        public async Task<IActionResult> Reject(
            Guid id,
            [FromBody] RejectCoachPayoutAccountRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _payoutAccountService.RejectAsync(adminId, id, request);
            return Ok(result);
        }
    }
}
