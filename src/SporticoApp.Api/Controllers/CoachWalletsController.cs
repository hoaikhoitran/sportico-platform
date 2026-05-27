using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Wallets;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = RoleConstants.Coach)]
    public class CoachWalletsController : ControllerBase
    {
        private readonly ICoachWalletService _coachWalletService;

        public CoachWalletsController(ICoachWalletService coachWalletService)
        {
            _coachWalletService = coachWalletService;
        }

        [HttpGet("api/coaches/me/wallet")]
        [ProducesResponseType(typeof(Result<CoachWalletResponse>), 200)]
        public async Task<IActionResult> GetWallet()
        {
            var coachId = User.GetUserId();
            var result = await _coachWalletService.GetMyWalletAsync(coachId);
            return Ok(result);
        }

        [HttpGet("api/coaches/me/wallet/transactions")]
        [ProducesResponseType(typeof(Result<PagedResult<CoachWalletTransactionResponse>>), 200)]
        public async Task<IActionResult> GetTransactions([FromQuery] CoachWalletTransactionFilterRequest filter)
        {
            var coachId = User.GetUserId();
            var result = await _coachWalletService.GetMyTransactionsAsync(coachId, filter);
            return Ok(result);
        }
    }
}
