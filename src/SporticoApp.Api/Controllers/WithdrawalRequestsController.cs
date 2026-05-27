using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Withdrawals;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class WithdrawalRequestsController : ControllerBase
    {
        private readonly IWithdrawalService _withdrawalService;

        public WithdrawalRequestsController(IWithdrawalService withdrawalService)
        {
            _withdrawalService = withdrawalService;
        }

        [HttpPost("api/coaches/me/withdrawal-requests")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<WithdrawalRequestResponse>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateWithdrawalRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _withdrawalService.CreateAsync(coachId, request);
            return Ok(result);
        }

        [HttpGet("api/coaches/me/withdrawal-requests")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<PagedResult<WithdrawalRequestResponse>>), 200)]
        public async Task<IActionResult> GetMy([FromQuery] WithdrawalRequestFilterRequest filter)
        {
            var coachId = User.GetUserId();
            var result = await _withdrawalService.GetMyAsync(coachId, filter);
            return Ok(result);
        }

        [HttpGet("api/admin/withdrawal-requests/pending")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<PagedResult<WithdrawalRequestResponse>>), 200)]
        public async Task<IActionResult> GetPending([FromQuery] WithdrawalRequestFilterRequest filter)
        {
            var result = await _withdrawalService.GetPendingAsync(filter);
            return Ok(result);
        }

        [HttpPut("api/admin/withdrawal-requests/{id:guid}/approve")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<WithdrawalRequestResponse>), 200)]
        public async Task<IActionResult> Approve(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _withdrawalService.ApproveAsync(adminId, id);
            return Ok(result);
        }

        [HttpPut("api/admin/withdrawal-requests/{id:guid}/reject")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<WithdrawalRequestResponse>), 200)]
        public async Task<IActionResult> Reject(
            Guid id,
            [FromBody] RejectWithdrawalRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _withdrawalService.RejectAsync(adminId, id, request);
            return Ok(result);
        }

        [HttpPut("api/admin/withdrawal-requests/{id:guid}/mark-paid")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<WithdrawalRequestResponse>), 200)]
        public async Task<IActionResult> MarkPaid(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _withdrawalService.MarkPaidAsync(adminId, id);
            return Ok(result);
        }
    }
}
