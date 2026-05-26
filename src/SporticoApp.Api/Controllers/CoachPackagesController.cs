using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Packages;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/coach-packages")]
    [Authorize(Roles = RoleConstants.Coach)]
    public class CoachPackagesController : ControllerBase
    {
        private readonly ICoachPackageService _coachPackageService;

        public CoachPackagesController(
            ICoachPackageService coachPackageService)
        {
            _coachPackageService = coachPackageService;
        }

        [HttpGet("me/current")]
        [ProducesResponseType(typeof(Result<CoachPackageResponse>), 200)]
        public async Task<IActionResult> GetCurrent()
        {
            var coachId = User.GetUserId();

            var result =
                await _coachPackageService.GetCurrentAsync(coachId);

            return Ok(result);
        }

        [HttpPost("purchase/payos")]
        [ProducesResponseType(typeof(Result<PurchaseCoachPackagePayOsResponse>), 200)]
        public async Task<IActionResult> PurchaseWithPayOs(
            [FromBody] PurchaseCoachPackageRequest request)
        {
            var coachId = User.GetUserId();

            var result =
                await _coachPackageService.PurchaseWithPayOsAsync(
                    coachId,
                    request);

            return Ok(result);
        }
    }
}