using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.PublicCoaches;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/public/coaches")]
    [AllowAnonymous]
    public class PublicCoachesController : ControllerBase
    {
        private readonly IPublicCoachService _publicCoachService;

        public PublicCoachesController(IPublicCoachService publicCoachService)
        {
            _publicCoachService = publicCoachService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<PublicCoachListItemResponse>>), 200)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] PublicCoachFilterRequest filter)
        {
            var result = await _publicCoachService.GetPagedAsync(filter);
            return Ok(result);
        }

        [HttpGet("{coachId:guid}")]
        [ProducesResponseType(typeof(Result<PublicCoachDetailResponse>), 200)]
        public async Task<IActionResult> GetById(Guid coachId)
        {
            var result = await _publicCoachService.GetByIdAsync(coachId);
            return Ok(result);
        }
    }
}