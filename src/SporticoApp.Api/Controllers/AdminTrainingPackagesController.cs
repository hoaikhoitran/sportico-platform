using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin/training-packages")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminTrainingPackagesController : ControllerBase
    {
        private readonly IAdminTrainingPackageService _trainingPackageService;

        public AdminTrainingPackagesController(IAdminTrainingPackageService trainingPackageService)
        {
            _trainingPackageService = trainingPackageService;
        }

        [HttpGet("pending")]
        [ProducesResponseType(typeof(Result<PagedResult<TrainingPackageResponse>>), 200)]
        public async Task<IActionResult> GetPending([FromQuery] TrainingPackageFilterRequest filter)
        {
            var result = await _trainingPackageService.GetPendingAsync(filter);
            return Ok(result);
        }

        [HttpPut("{id:guid}/approve")]
        [ProducesResponseType(typeof(Result<TrainingPackageResponse>), 200)]
        public async Task<IActionResult> Approve(Guid id)
        {
            var adminId = User.GetUserId();
            var result = await _trainingPackageService.ApproveAsync(adminId, id);
            return Ok(result);
        }

        [HttpPut("{id:guid}/reject")]
        [ProducesResponseType(typeof(Result<TrainingPackageResponse>), 200)]
        public async Task<IActionResult> Reject(
            Guid id,
            [FromBody] RejectTrainingPackageRequest request)
        {
            var adminId = User.GetUserId();
            var result = await _trainingPackageService.RejectAsync(adminId, id, request);
            return Ok(result);
        }
    }
}
