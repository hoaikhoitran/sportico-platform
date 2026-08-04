using SporticoApp.Application.DTOs.Community;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICommunityReportService
    {
        Task<Result<ReportResponse>> CreateAsync(Guid reporterId, CreateReportRequest request);
    }
}
