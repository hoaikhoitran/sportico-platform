using SporticoApp.Application.DTOs.Community;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICommunityCommentService
    {
        Task<Result<PagedResult<CommunityCommentResponse>>> GetCommentsAsync(
            Guid? currentUserId, Guid postId, CommunityCommentFilterRequest filter);

        Task<Result<CommunityCommentResponse>> AddCommentAsync(Guid userId, Guid postId, CreateCommentRequest request);

        Task<Result<CommunityCommentResponse>> AddReplyAsync(Guid userId, Guid parentCommentId, CreateReplyRequest request);

        Task<Result<CommunityCommentResponse>> UpdateCommentAsync(Guid userId, Guid commentId, UpdateCommentRequest request);

        Task<Result<object>> DeleteCommentAsync(Guid userId, Guid commentId);
    }
}
