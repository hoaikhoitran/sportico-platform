using SporticoApp.Application.DTOs.Community;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICommunityPostRepository
    {
        /// <summary>Read-only, includes Author/Sport/Media. Used for public/author detail.</summary>
        Task<CommunityPost?> GetByIdAsync(Guid id);

        /// <summary>Tracked, includes Media. Used for update/moderation/application-count mutations.</summary>
        Task<CommunityPost?> GetByIdForUpdateAsync(Guid id);

        Task<(List<CommunityPost> Items, int TotalCount)> GetPagedAsync(CommunityPostFilterRequest filter, Guid? currentUserId);

        Task<(List<CommunityPost> Items, int TotalCount)> GetPagedByAuthorAsync(Guid authorId, CommunityPostFilterRequest filter);

        Task<(List<CommunityPost> Items, int TotalCount)> GetPagedForAdminAsync(AdminCommunityPostFilterRequest filter);

        /// <summary>Published/closed posts whose EndAt (or a day past StartAt when no EndAt) has passed — for the expiry sweep.</summary>
        Task<List<CommunityPost>> GetExpiryCandidatesAsync(DateTime nowUtc, int batchSize);

        Task AddWithoutSaveAsync(CommunityPost post);

        Task<int> IncrementViewCountAsync(Guid postId);

        Task SaveChangesAsync();
    }
}
