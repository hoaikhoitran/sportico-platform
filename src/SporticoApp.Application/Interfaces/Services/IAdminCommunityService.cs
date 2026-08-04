using SporticoApp.Application.DTOs.Community;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IAdminCommunityService
    {
        Task<Result<PagedResult<AdminCommunityPostResponse>>> GetPostsAsync(AdminCommunityPostFilterRequest filter);

        Task<Result<CommunityPostResponse>> GetPostByIdAsync(Guid postId);

        Task<Result<CommunityPostResponse>> HidePostAsync(Guid adminId, Guid postId, HideContentRequest request);

        Task<Result<CommunityPostResponse>> RestorePostAsync(Guid adminId, Guid postId);

        Task<Result<object>> DeletePostAsync(Guid adminId, Guid postId);

        Task<Result<PagedResult<CommunityCommentResponse>>> GetCommentsAsync(Guid postId, CommunityCommentFilterRequest filter);

        Task<Result<CommunityCommentResponse>> HideCommentAsync(Guid adminId, Guid commentId, HideContentRequest request);

        Task<Result<CommunityCommentResponse>> RestoreCommentAsync(Guid adminId, Guid commentId);

        Task<Result<object>> DeleteCommentAsync(Guid adminId, Guid commentId);

        Task<Result<PagedResult<ReportResponse>>> GetReportsAsync(AdminReportFilterRequest filter);

        Task<Result<ReportResponse>> ResolveReportAsync(Guid adminId, Guid reportId, ResolveReportRequest request);
    }
}
