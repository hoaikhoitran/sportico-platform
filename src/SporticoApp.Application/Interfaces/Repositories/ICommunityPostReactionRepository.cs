using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICommunityPostReactionRepository
    {
        Task<CommunityPostReaction?> GetAsync(Guid postId, Guid userId);

        Task AddWithoutSaveAsync(CommunityPostReaction reaction);

        Task RemoveWithoutSaveAsync(CommunityPostReaction reaction);

        Task SaveChangesAsync();
    }
}
