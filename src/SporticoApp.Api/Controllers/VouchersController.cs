using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    /// <summary>Learner-facing voucher endpoints. Validate is a read-only preview — it never reserves a seat.</summary>
    [ApiController]
    [Route("api/vouchers")]
    [Authorize]
    public class VouchersController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VouchersController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(Result<VoucherQuoteResponse>), 200)]
        public async Task<IActionResult> Validate([FromBody] ValidateVoucherRequest request)
        {
            var userId = User.GetUserId();
            var result = await _voucherService.ValidateAsync(userId, request);
            return Ok(result);
        }
    }
}
