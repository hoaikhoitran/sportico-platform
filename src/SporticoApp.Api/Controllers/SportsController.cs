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

        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<SportResponse>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateSportRequest request)
        {
            var result = await _sportService.CreateAsync(request);
            return Ok(result);
        }
    }
}
