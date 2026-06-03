using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = RoleConstants.Admin)]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        [HttpGet("api/admin/users")]
        [ProducesResponseType(typeof(Result<PagedResult<AdminUserResponse>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] AdminUserFilterRequest filter)
        {
            var result = await _adminUserService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("api/admin/users/{id:guid}")]
        [ProducesResponseType(typeof(Result<AdminUserResponse>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _adminUserService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost("api/admin/users")]
        [ProducesResponseType(typeof(Result<AdminUserResponse>), 200)]
        public async Task<IActionResult> Create([FromBody] AdminCreateUserRequest request)
        {
            var result = await _adminUserService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("api/admin/users/{id:guid}")]
        [ProducesResponseType(typeof(Result<AdminUserResponse>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] AdminUpdateUserRequest request)
        {
            var result = await _adminUserService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("api/admin/users/{id:guid}")]
        [ProducesResponseType(typeof(Result<AdminUserResponse>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _adminUserService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
