using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IUserBlockRepository
    {
        Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedUserId);

        /// <summary>True if EITHER user has blocked the other.</summary>
        Task<bool> IsBlockedEitherDirectionAsync(Guid userId1, Guid userId2);

        Task<UserBlock?> GetAsync(Guid blockerId, Guid blockedUserId);

        Task<List<UserBlock>> GetBlockedByUserAsync(Guid blockerId);

        Task AddAsync(UserBlock block);

        Task RemoveAsync(UserBlock block);
    }
}
