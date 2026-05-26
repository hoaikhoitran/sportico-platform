using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.Sports;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SportsController : ControllerBase
    {
        private readonly ISportService _sportService;

        public SportsController(ISportService sportService)
        {
            _sportService = sportService;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<PagedResult<SportResponse>>), 200)]
        public async Task<IActionResult> GetSports(
            [FromQuery] SportFilterRequest filter)
        {
            var result = await _sportService.GetPagedAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<SportResponse>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _sportService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<SportResponse>), 200)]
        public async Task<IActionResult> Create(
            [FromBody] CreateSportRequest request)
        {
            var result = await _sportService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<SportResponse>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateSportStatusRequest request)
        {
            var result = await _sportService.UpdateStatusAsync(id, request);
            return Ok(result);
        }
    }
}