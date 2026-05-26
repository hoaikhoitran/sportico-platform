using FluentValidation;
using SporticoApp.Application.DTOs.Posts;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class AdminPostService : IAdminPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IValidator<PostFilterRequest> _filterValidator;

        public AdminPostService(
            IPostRepository postRepository,
            IValidator<PostFilterRequest> filterValidator)
        {
            _postRepository = postRepository;
            _filterValidator = filterValidator;
        }

        public async Task<Result<PagedResult<PostResponse>>> GetPendingPostsAsync(
            PostFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var (items, totalCount) = await _postRepository.GetAdminPendingPagedAsync(filter);

            var response = new PagedResult<PostResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<PostResponse>>.Success(response);
        }

        public async Task<Result<PostResponse>> ApproveAsync(Guid postId)
        {
            var post = await _postRepository.GetByIdForUpdateAsync(postId);

            if (post == null)
            {
                throw new NotFoundException(
                    ErrorCodes.PostNotFound,
                    "Post not found");
            }

            if (post.Status != PostStatusConstants.Pending &&
                post.Status != PostStatusConstants.Draft)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidPostStatus,
                    "Post status is invalid for approval");
            }

            post.Status = PostStatusConstants.Published;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            var updated = await _postRepository.GetByIdAsync(postId);

            return Result<PostResponse>.Success(updated!.ToResponse());
        }

        public async Task<Result<PostResponse>> RejectAsync(
            Guid postId,
            RejectPostRequest request)
        {
            var post = await _postRepository.GetByIdForUpdateAsync(postId);

            if (post == null)
            {
                throw new NotFoundException(
                    ErrorCodes.PostNotFound,
                    "Post not found");
            }

            if (post.Status != PostStatusConstants.Pending &&
                post.Status != PostStatusConstants.Draft)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidPostStatus,
                    "Post status is invalid for rejection");
            }

            post.Status = PostStatusConstants.Rejected;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            var updated = await _postRepository.GetByIdAsync(postId);

            return Result<PostResponse>.Success(updated!.ToResponse());
        }
    }
}
