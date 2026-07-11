using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.PlatformSettings;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin/platform-settings")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminPlatformSettingsController : ControllerBase
    {
        private readonly IPlatformSettingService _platformSettingService;

        public AdminPlatformSettingsController(IPlatformSettingService platformSettingService)
        {
            _platformSettingService = platformSettingService;
        }

        [HttpGet("commission")]
        [ProducesResponseType(typeof(Result<PlatformCommissionResponse>), 200)]
        public async Task<IActionResult> GetCommission()
        {
            var result = await _platformSettingService.GetCommissionAsync();
            return Ok(result);
        }

        [HttpPut("commission")]
        [ProducesResponseType(typeof(Result<PlatformCommissionResponse>), 200)]
        public async Task<IActionResult> UpdateCommission(
            [FromBody] UpdatePlatformCommissionRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _platformSettingService.UpdateCommissionAsync(adminId, request);
            return Ok(result);
        }
    }
}
