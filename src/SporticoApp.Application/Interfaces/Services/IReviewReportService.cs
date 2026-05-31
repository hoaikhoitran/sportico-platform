using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IReviewReportService
    {
        /// <summary>Coach reports a review written about them.</summary>
        Task<Result<ReviewReportResponse>> ReportAsync(
            Guid coachId,
            Guid reviewId,
            CreateReviewReportRequest request);

        /// <summary>Admin: list review reports, optionally filtered by status.</summary>
        Task<Result<PagedResult<ReviewReportResponse>>> GetReportsAsync(
            ReviewReportFilterRequest filter);

        /// <summary>Admin: resolve a report — reject it, or accept it and hide the review.</summary>
        Task<Result<ReviewReportResponse>> ResolveAsync(
            Guid adminId,
            Guid reportId,
            ResolveReviewReportRequest request);
    }
}
