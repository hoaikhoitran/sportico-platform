using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin/voucher-campaigns")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminVoucherCampaignsController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public AdminVoucherCampaignsController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(Result<VoucherCampaignResponse>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateVoucherCampaignRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _voucherService.CreateCampaignAsync(adminId, request);
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<VoucherCampaignResponse>>), 200)]
        public async Task<IActionResult> GetList([FromQuery] VoucherCampaignFilterRequest filter)
        {
            var result = await _voucherService.GetCampaignsAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<VoucherCampaignResponse>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _voucherService.GetCampaignByIdAsync(id);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<VoucherCampaignResponse>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVoucherCampaignRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _voucherService.UpdateCampaignAsync(adminId, id, request);
            return Ok(result);
        }

        [HttpPut("{id:guid}/activate")]
        [ProducesResponseType(typeof(Result<VoucherCampaignResponse>), 200)]
        public async Task<IActionResult> Activate(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _voucherService.ActivateCampaignAsync(adminId, id);
            return Ok(result);
        }

        [HttpPut("{id:guid}/pause")]
        [ProducesResponseType(typeof(Result<VoucherCampaignResponse>), 200)]
        public async Task<IActionResult> Pause(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _voucherService.PauseCampaignAsync(adminId, id);
            return Ok(result);
        }

        [HttpPut("{id:guid}/end")]
        [ProducesResponseType(typeof(Result<VoucherCampaignResponse>), 200)]
        public async Task<IActionResult> End(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _voucherService.EndCampaignAsync(adminId, id);
            return Ok(result);
        }

        [HttpGet("{id:guid}/redemptions")]
        [ProducesResponseType(typeof(Result<PagedResult<VoucherRedemptionResponse>>), 200)]
        public async Task<IActionResult> GetRedemptions(Guid id, [FromQuery] VoucherRedemptionFilterRequest filter)
        {
            var result = await _voucherService.GetRedemptionsAsync(id, filter);
            return Ok(result);
        }
    }
}
