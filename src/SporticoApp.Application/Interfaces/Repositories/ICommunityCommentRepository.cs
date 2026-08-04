using SporticoApp.Application.DTOs.Community;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICommunityCommentRepository
    {
        Task<CommunityComment?> GetByIdForUpdateAsync(Guid id);

        /// <summary>Root comments (active/hidden, never deleted-hard) with their replies, newest first.</summary>
        Task<(List<CommunityComment> Items, int TotalCount)> GetRootCommentsPagedAsync(
            Guid postId, CommunityCommentFilterRequest filter);

        Task<(List<CommunityComment> Items, int TotalCount)> GetForAdminPagedAsync(
            Guid postId, CommunityCommentFilterRequest filter);

        Task AddWithoutSaveAsync(CommunityComment comment);

        Task SaveChangesAsync();
    }
}
