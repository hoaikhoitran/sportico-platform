using SporticoApp.Application.DTOs.Community;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    /// <summary>
    /// Reports whose TargetType is community_post or community_comment — reuses the shared
    /// <see cref="Report"/> table (same pattern as IReviewReportRepository for reviews).
    /// </summary>
    public interface ICommunityReportRepository
    {
        Task<Report?> GetByIdForUpdateAsync(Guid id);

        Task<Report?> GetOpenReportAsync(string targetType, Guid targetId, Guid reporterId);

        Task<int> CountOpenByTargetAsync(string targetType, Guid targetId);

        Task<(List<Report> Items, int TotalCount)> GetPagedAsync(AdminReportFilterRequest filter);

        Task AddWithoutSaveAsync(Report report);

        Task SaveChangesAsync();
    }
}
