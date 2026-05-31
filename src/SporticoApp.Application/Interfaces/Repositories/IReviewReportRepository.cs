using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IReviewReportRepository
    {
        Task<Report?> GetByIdAsync(Guid id);

        Task<Report?> GetByIdForUpdateAsync(Guid id);

        /// <summary>An open (pending/reviewing) review report by this reporter for the same review.</summary>
        Task<Report?> GetOpenReportAsync(Guid reviewId, Guid reporterId);

        Task<(List<Report> Items, int TotalCount)> GetPagedReviewReportsAsync(
            ReviewReportFilterRequest filter);

        Task AddAsync(Report report);

        Task SaveChangesAsync();
    }
}
