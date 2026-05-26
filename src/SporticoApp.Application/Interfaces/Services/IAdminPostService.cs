using SporticoApp.Application.DTOs.Posts;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IAdminPostService
    {
        Task<Result<PagedResult<PostResponse>>> GetPendingPostsAsync(
            PostFilterRequest filter);

        Task<Result<PostResponse>> ApproveAsync(Guid postId);

        Task<Result<PostResponse>> RejectAsync(Guid postId, RejectPostRequest request);
    }
}
