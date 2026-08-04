using SporticoApp.Application.DTOs.Community;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICommunityPostApplicationRepository
    {
        Task<CommunityPostApplication?> GetByIdForUpdateAsync(Guid id);

        Task<CommunityPostApplication?> GetByPostAndApplicantAsync(Guid postId, Guid applicantId);

        Task<(List<CommunityPostApplication> Items, int TotalCount)> GetPagedByPostAsync(
            Guid postId, CommunityApplicationFilterRequest filter);

        Task AddWithoutSaveAsync(CommunityPostApplication application);

        Task SaveChangesAsync();
    }
}
