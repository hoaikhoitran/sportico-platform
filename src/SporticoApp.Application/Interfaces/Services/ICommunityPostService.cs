using SporticoApp.Application.DTOs.Community;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICommunityPostService
    {
        Task<Result<CommunityPostResponse>> CreateAsync(Guid authorId, CreateCommunityPostRequest request);

        Task<Result<CommunityPostResponse>> UpdateAsync(Guid authorId, Guid postId, UpdateCommunityPostRequest request);

        Task<Result<CommunityPostResponse>> CloseAsync(Guid authorId, Guid postId);

        Task<Result<object>> DeleteAsync(Guid authorId, Guid postId);

        Task<Result<PagedResult<CommunityPostResponse>>> GetFeedAsync(Guid? currentUserId, CommunityPostFilterRequest filter);

        Task<Result<PagedResult<CommunityPostResponse>>> GetMyPostsAsync(Guid authorId, CommunityPostFilterRequest filter);

        Task<Result<CommunityPostResponse>> GetByIdAsync(Guid? currentUserId, Guid postId);

        Task<Result<object>> LikeAsync(Guid userId, Guid postId);

        Task<Result<object>> UnlikeAsync(Guid userId, Guid postId);

        Task<Result<CommunityApplicationResponse>> ApplyAsync(Guid userId, Guid postId, CreateApplicationRequest request);

        Task<Result<object>> CancelMyApplicationAsync(Guid userId, Guid postId);

        Task<Result<PagedResult<CommunityApplicationResponse>>> GetApplicationsAsync(
            Guid ownerId, Guid postId, CommunityApplicationFilterRequest filter);

        Task<Result<CommunityApplicationResponse>> AcceptApplicationAsync(Guid ownerId, Guid applicationId);

        Task<Result<CommunityApplicationResponse>> RejectApplicationAsync(Guid ownerId, Guid applicationId);
    }
}
