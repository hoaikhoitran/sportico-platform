using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.Packages;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/packages")]
    public class PackagesController : ControllerBase
    {
        private readonly IPackageService _packageService;

        public PackagesController(IPackageService packageService)
        {
            _packageService = packageService;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<PagedResult<PackageResponse>>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] PackageFilterRequest filter)
        {
            var result = await _packageService.GetPagedAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<PackageResponse>), 200)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _packageService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<PackageResponse>), 200)]
        public async Task<IActionResult> Create([FromBody] CreatePackageRequest request)
        {
            var result = await _packageService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<PackageResponse>), 200)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdatePackageRequest request)
        {
            var result = await _packageService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(Result<PackageResponse>), 200)]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdatePackageStatusRequest request)
        {
            var result = await _packageService.UpdateStatusAsync(id, request);
            return Ok(result);
        }
    }
}
