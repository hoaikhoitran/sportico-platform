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
    [Route("api/training-packages")]
    [Authorize(Roles = RoleConstants.Coach)]
    public class TrainingPackagesController : ControllerBase
    {
        private readonly ITrainingPackageService _trainingPackageService;

        public TrainingPackagesController(ITrainingPackageService trainingPackageService)
        {
            _trainingPackageService = trainingPackageService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(Result<TrainingPackageResponse>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateTrainingPackageRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPackageService.CreateAsync(coachId, request);
            return Ok(result);
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(Result<PagedResult<TrainingPackageResponse>>), 200)]
        public async Task<IActionResult> GetMyPackages([FromQuery] TrainingPackageFilterRequest filter)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPackageService.GetMyPagedAsync(coachId, filter);
            return Ok(result);
        }

        [HttpGet("me/{id:guid}")]
        [ProducesResponseType(typeof(Result<TrainingPackageResponse>), 200)]
        public async Task<IActionResult> GetMyPackageById(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPackageService.GetMyByIdAsync(coachId, id);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<TrainingPackageResponse>), 200)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateTrainingPackageRequest request)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPackageService.UpdateAsync(coachId, id, request);
            return Ok(result);
        }

        [HttpPut("{id:guid}/archive")]
        [ProducesResponseType(typeof(Result<TrainingPackageResponse>), 200)]
        public async Task<IActionResult> Archive(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _trainingPackageService.ArchiveAsync(coachId, id);
            return Ok(result);
        }
    }
}
