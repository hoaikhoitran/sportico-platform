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

    public class PostService : IPostService
    {
        private readonly ICoachRepository _coachRepository;
        private readonly ISportRepository _sportRepository;
        private readonly ICoachPackageRepository _coachPackageRepository;
        private readonly IPostRepository _postRepository;
        private readonly IValidator<CreatePostRequest> _createValidator;
        private readonly IValidator<UpdatePostRequest> _updateValidator;
        private readonly IValidator<PostFilterRequest> _filterValidator;

        public PostService(
            ICoachRepository coachRepository,
            ISportRepository sportRepository,
            ICoachPackageRepository coachPackageRepository,
            IPostRepository postRepository,
            IValidator<CreatePostRequest> createValidator,
            IValidator<UpdatePostRequest> updateValidator,
            IValidator<PostFilterRequest> filterValidator)
        {
            _coachRepository = coachRepository;
            _sportRepository = sportRepository;
            _coachPackageRepository = coachPackageRepository;
            _postRepository = postRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _filterValidator = filterValidator;
        }

        public async Task<Result<PostResponse>> CreateAsync(
            Guid coachId,
            CreatePostRequest request)
        {
            request.ImageUrls ??= new List<string>();

            var validationResult = await _createValidator.ValidateAsync(request);
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

            var coachExists = await _coachRepository.ExistsByUserIdAsync(coachId);
            if (!coachExists)
            {
                throw new ForbiddenException(
                    ErrorCodes.CoachProfileRequired,
                    "You must register as a coach first");
            }

            var sport = await _sportRepository.GetByIdAsync(request.SportId);
            if (sport == null || !sport.IsActive)
            {
                throw new ValidationException(
                    ErrorCodes.InvalidSport,
                    "Sport is invalid");
            }

            var currentPackage =
                await _coachPackageRepository.GetCurrentForUpdateAsync(coachId);

            var now = DateTime.UtcNow;

            if (currentPackage == null ||
                currentPackage.Status != CoachPackageStatuses.Active ||
                currentPackage.EndDate <= now)
            {
                if (currentPackage != null && currentPackage.EndDate <= now)
                {
                    currentPackage.Status = CoachPackageStatuses.Expired;
                    await _coachPackageRepository.SaveChangesAsync();
                }

                throw new ConflictException(
                    ErrorCodes.ActivePackageRequired,
                    "Active package is required");
            }

            if (currentPackage.RemainingPosts <= 0)
            {
                currentPackage.Status = CoachPackageStatuses.Expired;
                await _coachPackageRepository.SaveChangesAsync();

                throw new ConflictException(
                    ErrorCodes.PostQuotaExceeded,
                    "Post quota exceeded");
            }

            var post = request.ToEntity(coachId);

            currentPackage.RemainingPosts -= 1;

            await _postRepository.AddAsync(post);

            // Assign the (no-tracking) sport only after persisting so EF does not
            // treat it as a new related entity and attempt to re-insert it.
            // ToEntity already sets the SportId FK; this is purely for the response.
            post.Sport = sport;

            return Result<PostResponse>.Success(post.ToResponse());
        }

        public async Task<Result<PagedResult<PostResponse>>> GetMyPostsAsync(
            Guid coachId,
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

            var (items, totalCount) = await _postRepository.GetMyPagedAsync(coachId, filter);

            var response = new PagedResult<PostResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<PostResponse>>.Success(response);
        }

        public async Task<Result<PostResponse>> GetMyPostByIdAsync(
            Guid coachId,
            Guid postId)
        {
            var post = await _postRepository.GetOwnedByIdAsync(coachId, postId);

            if (post == null)
            {
                throw new NotFoundException(
                    ErrorCodes.PostNotFound,
                    "Post not found");
            }

            return Result<PostResponse>.Success(post.ToResponse());
        }

        public async Task<Result<PostResponse>> UpdateAsync(
            Guid coachId,
            Guid postId,
            UpdatePostRequest request)
        {
            request.ImageUrls ??= new List<string>();

            var validationResult = await _updateValidator.ValidateAsync(request);
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

            var post = await _postRepository.GetOwnedByIdForUpdateAsync(coachId, postId);

            if (post == null)
            {
                var existing = await _postRepository.GetByIdAsync(postId);

                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.PostNotOwned,
                        "Post is not owned by the current coach");
                }

                throw new NotFoundException(
                    ErrorCodes.PostNotFound,
                    "Post not found");
            }

            if (post.Status != PostStatusConstants.Pending &&
                post.Status != PostStatusConstants.Draft &&
                post.Status != PostStatusConstants.Rejected)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidPostStatus,
                    "Post status is invalid for update");
            }

            var sport = await _sportRepository.GetByIdAsync(request.SportId);
            if (sport == null || !sport.IsActive)
            {
                throw new ValidationException(
                    ErrorCodes.InvalidSport,
                    "Sport is invalid");
            }

            post.ApplyUpdate(request);

            await _postRepository.SaveChangesAsync();

            var updated = await _postRepository.GetOwnedByIdAsync(coachId, postId);

            return Result<PostResponse>.Success(updated!.ToResponse());
        }

        public async Task<Result<PostResponse>> ArchiveAsync(
            Guid coachId,
            Guid postId)
        {
            var post = await _postRepository.GetOwnedByIdForUpdateAsync(coachId, postId);

            if (post == null)
            {
                var existing = await _postRepository.GetByIdAsync(postId);

                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.PostNotOwned,
                        "Post is not owned by the current coach");
                }

                throw new NotFoundException(
                    ErrorCodes.PostNotFound,
                    "Post not found");
            }

            post.Status = PostStatusConstants.Archived;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            var updated = await _postRepository.GetOwnedByIdAsync(coachId, postId);

            return Result<PostResponse>.Success(updated!.ToResponse());
        }
    }
}
