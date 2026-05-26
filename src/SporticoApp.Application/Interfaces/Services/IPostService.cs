using SporticoApp.Application.DTOs.Posts;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IPostService
    {
        Task<Result<PostResponse>> CreateAsync(Guid coachId, CreatePostRequest request);

        Task<Result<PagedResult<PostResponse>>> GetMyPostsAsync(
            Guid coachId,
            PostFilterRequest filter);

        Task<Result<PostResponse>> GetMyPostByIdAsync(Guid coachId, Guid postId);

        Task<Result<PostResponse>> UpdateAsync(
            Guid coachId,
            Guid postId,
            UpdatePostRequest request);

        Task<Result<PostResponse>> ArchiveAsync(Guid coachId, Guid postId);
    }
}
