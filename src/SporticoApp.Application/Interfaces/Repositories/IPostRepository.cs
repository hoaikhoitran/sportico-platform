using SporticoApp.Application.DTOs.Posts;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(Guid id);

        Task<Post?> GetByIdForUpdateAsync(Guid id);

        Task<Post?> GetOwnedByIdAsync(Guid coachId, Guid postId);

        Task<Post?> GetOwnedByIdForUpdateAsync(Guid coachId, Guid postId);

        Task<(List<Post> Items, int TotalCount)> GetMyPagedAsync(
            Guid coachId,
            PostFilterRequest filter);

        Task<(List<Post> Items, int TotalCount)> GetAdminPendingPagedAsync(
            PostFilterRequest filter);

        Task AddAsync(Post post);

        Task SaveChangesAsync();
    }
}
