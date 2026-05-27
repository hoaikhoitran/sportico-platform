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
    [Route("api/coaches/me/payout-account")]
    [Authorize(Roles = RoleConstants.Coach)]
    public class CoachPayoutAccountsController : ControllerBase
    {
        private readonly ICoachPayoutAccountService _payoutAccountService;

        public CoachPayoutAccountsController(ICoachPayoutAccountService payoutAccountService)
        {
            _payoutAccountService = payoutAccountService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<CoachPayoutAccountResponse>), 200)]
        public async Task<IActionResult> Get()
        {
            var coachId = User.GetUserId();
            var result = await _payoutAccountService.GetMyAsync(coachId);
            return Ok(result);
        }

        [HttpPut]
        [ProducesResponseType(typeof(Result<CoachPayoutAccountResponse>), 200)]
        public async Task<IActionResult> Upsert([FromBody] UpsertCoachPayoutAccountRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _payoutAccountService.UpsertAsync(coachId, request);
            return Ok(result);
        }
    }
}
