using SporticoApp.Application.DTOs.Users;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IUserBlockService
    {
        Task<Result<object>> BlockAsync(Guid blockerId, Guid targetUserId, BlockUserRequest request);

        Task<Result<object>> UnblockAsync(Guid blockerId, Guid targetUserId);

        Task<Result<List<BlockedUserResponse>>> GetBlockedAsync(Guid blockerId);
    }
}
