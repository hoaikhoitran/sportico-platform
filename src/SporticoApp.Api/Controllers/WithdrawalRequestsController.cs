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

        // ── Coach endpoints ───────────────────────────────────────────────────

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

        [HttpGet("api/coaches/me/withdrawal-requests/{id:guid}")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<WithdrawalRequestResponse>), 200)]
        public async Task<IActionResult> GetMyById(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _withdrawalService.GetMyByIdAsync(coachId, id);
            return Ok(result);
        }

        [HttpGet("api/coaches/me/withdrawal-requests/{id:guid}/receipt")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<WithdrawalReceiptResponse>), 200)]
        public async Task<IActionResult> GetReceipt(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _withdrawalService.GetReceiptAsync(coachId, id);
            return Ok(result);
        }

        // ── Admin endpoints ───────────────────────────────────────────────────

        // Full admin list with status filtering across every status.
        [HttpGet("api/admin/withdrawal-requests")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<PagedResult<WithdrawalRequestResponse>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] WithdrawalRequestFilterRequest filter)
        {
            var result = await _withdrawalService.GetAllAsync(filter);
            return Ok(result);
        }

        // Kept for backward compatibility — equivalent to GetAll with status=pending.
        [HttpGet("api/admin/withdrawal-requests/pending")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<PagedResult<WithdrawalRequestResponse>>), 200)]
        public async Task<IActionResult> GetPending([FromQuery] WithdrawalRequestFilterRequest filter)
        {
            var result = await _withdrawalService.GetPendingAsync(filter);
            return Ok(result);
        }

        // Single withdrawal detail for admin review modals (not the receipt view).
        [HttpGet("api/admin/withdrawal-requests/{id:guid}")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<WithdrawalRequestResponse>), 200)]
        public async Task<IActionResult> GetByIdAdmin(Guid id)
        {
            var result = await _withdrawalService.GetByIdAdminAsync(id);
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
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectWithdrawalRequest request)
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

        [HttpPut("api/admin/withdrawal-requests/{id:guid}/refresh-payout-status")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<WithdrawalRequestResponse>), 200)]
        public async Task<IActionResult> RefreshPayoutStatus(Guid id)
        {
            var result = await _withdrawalService.RefreshPayoutStatusAsync(id);
            return Ok(result);
        }

        [HttpPost("api/admin/withdrawal-requests/{id:guid}/retry-payout")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<WithdrawalRequestResponse>), 200)]
        public async Task<IActionResult> RetryPayout(Guid id)
        {
            var result = await _withdrawalService.RetryPayoutAsync(id);
            return Ok(result);
        }

        [HttpGet("api/admin/withdrawal-requests/{id:guid}/receipt")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<WithdrawalReceiptResponse>), 200)]
        public async Task<IActionResult> GetReceiptAdmin(Guid id)
        {
            var result = await _withdrawalService.GetReceiptAdminAsync(id);
            return Ok(result);
        }
    }
}
