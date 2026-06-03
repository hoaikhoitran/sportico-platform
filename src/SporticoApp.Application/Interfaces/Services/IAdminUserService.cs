using SporticoApp.Application.DTOs.Users;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IAdminUserService
    {
        Task<Result<PagedResult<AdminUserResponse>>> GetAllAsync(AdminUserFilterRequest filter);

        Task<Result<AdminUserResponse>> GetByIdAsync(Guid id);

        Task<Result<AdminUserResponse>> CreateAsync(AdminCreateUserRequest request);

        Task<Result<AdminUserResponse>> UpdateAsync(Guid id, AdminUpdateUserRequest request);

        /// <summary>Admin deactivation (status-based) — never a physical delete.</summary>
        Task<Result<AdminUserResponse>> DeleteAsync(Guid id);
    }
}
