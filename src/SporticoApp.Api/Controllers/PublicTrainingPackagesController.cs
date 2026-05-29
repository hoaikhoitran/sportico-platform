using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/public/training-packages")]
    [AllowAnonymous]
    public class PublicTrainingPackagesController : ControllerBase
    {
        private readonly IPublicTrainingPackageService _trainingPackageService;

        public PublicTrainingPackagesController(IPublicTrainingPackageService trainingPackageService)
        {
            _trainingPackageService = trainingPackageService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<PublicTrainingPackageResponse>>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] TrainingPackageFilterRequest filter)
        {
            var result = await _trainingPackageService.GetPagedAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<PublicTrainingPackageResponse>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _trainingPackageService.GetByIdAsync(id);
            return Ok(result);
        }
    }
}
