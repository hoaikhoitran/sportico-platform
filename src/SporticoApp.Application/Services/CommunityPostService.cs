using FluentValidation;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class CommunityPostService : ICommunityPostService
    {
        private readonly ICommunityPostRepository _postRepository;
        private readonly ICommunityPostReactionRepository _reactionRepository;
        private readonly ICommunityPostApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISportRepository _sportRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<CreateCommunityPostRequest> _createValidator;
        private readonly IValidator<UpdateCommunityPostRequest> _updateValidator;
        private readonly IValidator<CommunityPostFilterRequest> _filterValidator;
        private readonly IValidator<CreateApplicationRequest> _applicationValidator;
        private readonly IValidator<CommunityApplicationFilterRequest> _applicationFilterValidator;

        public CommunityPostService(
            ICommunityPostRepository postRepository,
            ICommunityPostReactionRepository reactionRepository,
            ICommunityPostApplicationRepository applicationRepository,
            IUserRepository userRepository,
            ISportRepository sportRepository,
            INotificationRepository notificationRepository,
            IValidator<CreateCommunityPostRequest> createValidator,
            IValidator<UpdateCommunityPostRequest> updateValidator,
            IValidator<CommunityPostFilterRequest> filterValidator,
            IValidator<CreateApplicationRequest> applicationValidator,
            IValidator<CommunityApplicationFilterRequest> applicationFilterValidator)
        {
            _postRepository = postRepository;
            _reactionRepository = reactionRepository;
            _applicationRepository = applicationRepository;
            _userRepository = userRepository;
            _sportRepository = sportRepository;
            _notificationRepository = notificationRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _filterValidator = filterValidator;
            _applicationValidator = applicationValidator;
            _applicationFilterValidator = applicationFilterValidator;
        }

        public async Task<Result<CommunityPostResponse>> CreateAsync(Guid authorId, CreateCommunityPostRequest request)
        {
            await ValidateOrThrowAsync(_createValidator, request);
            await EnsureActiveUserAsync(authorId);

            if (request.SportId.HasValue && await _sportRepository.GetByIdAsync(request.SportId.Value) == null)
            {
                throw new NotFoundException(ErrorCodes.SportNotFound, "Sport not found");
            }

            var isRecruitment = CommunityPostTypes.IsRecruitment(request.PostType);
            var now = DateTime.UtcNow;

            var post = new CommunityPost
            {
                Id = Guid.NewGuid(),
                AuthorId = authorId,
                SportId = request.SportId,
                PostType = request.PostType,
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                LocationName = request.LocationName,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                MaxParticipants = request.MaxParticipants,
                AcceptedParticipants = isRecruitment ? 1 : 0,
                Level = request.Level,
                FeePerPerson = request.FeePerPerson,
                Status = request.Publish ? CommunityPostStatuses.Published : CommunityPostStatuses.Draft,
                AllowComments = request.AllowComments,
                PublishedAt = request.Publish ? now : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            AttachMedia(post, request.Media);

            await _postRepository.AddWithoutSaveAsync(post);
            await _postRepository.SaveChangesAsync();

            var author = await _userRepository.GetByIdAsync(authorId);
            post.Author = author!;

            return Result<CommunityPostResponse>.Success(post.ToResponse(authorId, false, null));
        }

        public async Task<Result<CommunityPostResponse>> UpdateAsync(Guid authorId, Guid postId, UpdateCommunityPostRequest request)
        {
            await ValidateOrThrowAsync(_updateValidator, request);

            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.AuthorId != authorId)
            {
                throw new ForbiddenException(ErrorCodes.CommunityPostNotOwned, "This post does not belong to you");
            }

            if (post.Status != CommunityPostStatuses.Draft && post.Status != CommunityPostStatuses.Published)
            {
                throw new ConflictException(ErrorCodes.CommunityPostInvalidStatus, "This post can no longer be edited");
            }

            if (request.MaxParticipants.HasValue && request.MaxParticipants.Value < post.AcceptedParticipants)
            {
                throw new ConflictException(
                    ErrorCodes.CommunityPostInvalidStatus,
                    $"maxParticipants cannot be lower than the {post.AcceptedParticipants} participant(s) already accepted");
            }

            if (request.Title != null) post.Title = request.Title.Trim();
            if (request.Content != null) post.Content = request.Content.Trim();
            if (request.LocationName != null) post.LocationName = request.LocationName;
            if (request.Address != null) post.Address = request.Address;
            if (request.Latitude.HasValue) post.Latitude = request.Latitude;
            if (request.Longitude.HasValue) post.Longitude = request.Longitude;
            if (request.StartAt.HasValue) post.StartAt = request.StartAt;
            if (request.EndAt.HasValue) post.EndAt = request.EndAt;
            if (request.MaxParticipants.HasValue) post.MaxParticipants = request.MaxParticipants;
            if (request.Level != null) post.Level = request.Level;
            if (request.FeePerPerson.HasValue) post.FeePerPerson = request.FeePerPerson;
            if (request.AllowComments.HasValue) post.AllowComments = request.AllowComments.Value;

            if (request.Media != null)
            {
                post.Media.Clear();
                AttachMedia(post, request.Media);
            }

            // A post that is full again due to the raised cap can leave "closed" automatically.
            if (post.Status == CommunityPostStatuses.Closed &&
                (post.MaxParticipants == null || post.AcceptedParticipants < post.MaxParticipants.Value))
            {
                post.Status = CommunityPostStatuses.Published;
            }

            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            return Result<CommunityPostResponse>.Success(post.ToResponse(authorId, false, null));
        }

        public async Task<Result<CommunityPostResponse>> CloseAsync(Guid authorId, Guid postId)
        {
            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.AuthorId != authorId)
            {
                throw new ForbiddenException(ErrorCodes.CommunityPostNotOwned, "This post does not belong to you");
            }

            if (post.Status != CommunityPostStatuses.Published)
            {
                throw new ConflictException(ErrorCodes.CommunityPostInvalidStatus, "Only a published post can be closed");
            }

            post.Status = CommunityPostStatuses.Closed;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            return Result<CommunityPostResponse>.Success(post.ToResponse(authorId, false, null));
        }

        public async Task<Result<object>> DeleteAsync(Guid authorId, Guid postId)
        {
            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.AuthorId != authorId)
            {
                throw new ForbiddenException(ErrorCodes.CommunityPostNotOwned, "This post does not belong to you");
            }

            if (post.Status == CommunityPostStatuses.Deleted)
            {
                return Result<object>.Success(new { deleted = true });
            }

            post.Status = CommunityPostStatuses.Deleted;
            post.DeletedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            return Result<object>.Success(new { deleted = true });
        }

        public async Task<Result<PagedResult<CommunityPostResponse>>> GetFeedAsync(Guid? currentUserId, CommunityPostFilterRequest filter)
        {
            await ValidateOrThrowAsync(_filterValidator, filter);

            var (items, totalCount) = await _postRepository.GetPagedAsync(filter, currentUserId);

            var response = await MapListAsync(items, currentUserId);

            return Result<PagedResult<CommunityPostResponse>>.Success(
                new PagedResult<CommunityPostResponse>(response, totalCount, filter.PageNumber, filter.PageSize));
        }

        public async Task<Result<PagedResult<CommunityPostResponse>>> GetMyPostsAsync(Guid authorId, CommunityPostFilterRequest filter)
        {
            await ValidateOrThrowAsync(_filterValidator, filter);

            var (items, totalCount) = await _postRepository.GetPagedByAuthorAsync(authorId, filter);

            var response = await MapListAsync(items, authorId);

            return Result<PagedResult<CommunityPostResponse>>.Success(
                new PagedResult<CommunityPostResponse>(response, totalCount, filter.PageNumber, filter.PageSize));
        }

        public async Task<Result<CommunityPostResponse>> GetByIdAsync(Guid? currentUserId, Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null || post.Status == CommunityPostStatuses.Deleted)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            var isOwner = currentUserId.HasValue && post.AuthorId == currentUserId.Value;

            if (post.Status == CommunityPostStatuses.Hidden && !isOwner)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.Status == CommunityPostStatuses.Draft && !isOwner)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            await _postRepository.IncrementViewCountAsync(postId);

            var reacted = currentUserId.HasValue && await _reactionRepository.GetAsync(postId, currentUserId.Value) != null;
            var applicationStatus = currentUserId.HasValue
                ? (await _applicationRepository.GetByPostAndApplicantAsync(postId, currentUserId.Value))?.Status
                : null;

            return Result<CommunityPostResponse>.Success(post.ToResponse(currentUserId, reacted, applicationStatus));
        }

        public async Task<Result<object>> LikeAsync(Guid userId, Guid postId)
        {
            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null || post.Status == CommunityPostStatuses.Deleted)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            var existing = await _reactionRepository.GetAsync(postId, userId);
            if (existing != null)
            {
                return Result<object>.Success(new { liked = true }); // idempotent
            }

            await _reactionRepository.AddWithoutSaveAsync(new CommunityPostReaction
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            post.ReactionCount++;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            return Result<object>.Success(new { liked = true });
        }

        public async Task<Result<object>> UnlikeAsync(Guid userId, Guid postId)
        {
            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            var existing = await _reactionRepository.GetAsync(postId, userId);
            if (existing == null)
            {
                return Result<object>.Success(new { liked = false }); // idempotent
            }

            await _reactionRepository.RemoveWithoutSaveAsync(existing);

            post.ReactionCount = Math.Max(0, post.ReactionCount - 1);
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            return Result<object>.Success(new { liked = false });
        }

        public async Task<Result<CommunityApplicationResponse>> ApplyAsync(Guid userId, Guid postId, CreateApplicationRequest request)
        {
            await ValidateOrThrowAsync(_applicationValidator, request);

            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null || post.Status == CommunityPostStatuses.Deleted)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.AuthorId == userId)
            {
                throw new ForbiddenException(ErrorCodes.CommunityApplicationNotAllowed, "You cannot apply to your own post");
            }

            if (!CommunityPostTypes.IsRecruitment(post.PostType))
            {
                throw new ConflictException(ErrorCodes.CommunityApplicationNotAllowed, "This post type does not accept applications");
            }

            if (post.Status != CommunityPostStatuses.Published)
            {
                throw new ConflictException(ErrorCodes.CommunityPostNotPublished, "This post is not open for applications");
            }

            if (post.StartAt.HasValue && post.StartAt.Value <= DateTime.UtcNow)
            {
                throw new ConflictException(ErrorCodes.CommunityPostExpired, "This activity has already started");
            }

            if (post.MaxParticipants.HasValue && post.AcceptedParticipants >= post.MaxParticipants.Value)
            {
                throw new ConflictException(ErrorCodes.CommunityPostFull, "This post is already full");
            }

            var existing = await _applicationRepository.GetByPostAndApplicantAsync(postId, userId);
            if (existing != null)
            {
                throw new ConflictException(ErrorCodes.CommunityApplicationAlreadyExists, "You have already applied to this post");
            }

            var application = new CommunityPostApplication
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                ApplicantId = userId,
                Message = request.Message,
                Status = CommunityApplicationStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _applicationRepository.AddWithoutSaveAsync(application);

            post.ApplicationCount++;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            await NotifyAsync(post.AuthorId, "New join request", $"Someone requested to join \"{post.Title}\"", NotificationTypeConstants.System);

            var applicant = await _userRepository.GetByIdAsync(userId);
            application.Applicant = applicant!;

            return Result<CommunityApplicationResponse>.Success(application.ToResponse());
        }

        public async Task<Result<object>> CancelMyApplicationAsync(Guid userId, Guid postId)
        {
            var application = await _applicationRepository.GetByPostAndApplicantAsync(postId, userId);
            if (application == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityApplicationNotFound, "Application not found");
            }

            if (application.Status == CommunityApplicationStatuses.Cancelled)
            {
                return Result<object>.Success(new { cancelled = true }); // idempotent
            }

            if (application.Status == CommunityApplicationStatuses.Rejected)
            {
                throw new ConflictException(ErrorCodes.CommunityApplicationNotPending, "This application was already rejected");
            }

            var wasAccepted = application.Status == CommunityApplicationStatuses.Accepted;

            application.Status = CommunityApplicationStatuses.Cancelled;
            application.CancelledAt = DateTime.UtcNow;

            Guid? postOwnerId = null;
            if (wasAccepted)
            {
                var post = await _postRepository.GetByIdForUpdateAsync(postId);
                if (post != null)
                {
                    post.AcceptedParticipants = Math.Max(0, post.AcceptedParticipants - 1);

                    // Re-open a closed post if it hasn't started yet and now has room again.
                    if (post.Status == CommunityPostStatuses.Closed &&
                        (post.StartAt == null || post.StartAt > DateTime.UtcNow))
                    {
                        post.Status = CommunityPostStatuses.Published;
                    }

                    post.UpdatedAt = DateTime.UtcNow;
                    postOwnerId = post.AuthorId;
                }
            }

            await _applicationRepository.SaveChangesAsync();

            if (postOwnerId.HasValue)
            {
                await NotifyAsync(postOwnerId.Value, "A participant left", "A participant left your activity", NotificationTypeConstants.System);
            }

            return Result<object>.Success(new { cancelled = true });
        }

        public async Task<Result<PagedResult<CommunityApplicationResponse>>> GetApplicationsAsync(
            Guid ownerId, Guid postId, CommunityApplicationFilterRequest filter)
        {
            await ValidateOrThrowAsync(_applicationFilterValidator, filter);

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.AuthorId != ownerId)
            {
                throw new ForbiddenException(ErrorCodes.CommunityPostNotOwned, "Only the post owner can view applications");
            }

            var (items, totalCount) = await _applicationRepository.GetPagedByPostAsync(postId, filter);

            return Result<PagedResult<CommunityApplicationResponse>>.Success(new PagedResult<CommunityApplicationResponse>(
                items.Select(x => x.ToResponse()).ToList(), totalCount, filter.PageNumber, filter.PageSize));
        }

        public Task<Result<CommunityApplicationResponse>> AcceptApplicationAsync(Guid ownerId, Guid applicationId)
            => RespondApplicationAsync(ownerId, applicationId, accept: true);

        public Task<Result<CommunityApplicationResponse>> RejectApplicationAsync(Guid ownerId, Guid applicationId)
            => RespondApplicationAsync(ownerId, applicationId, accept: false);

        private async Task<Result<CommunityApplicationResponse>> RespondApplicationAsync(Guid ownerId, Guid applicationId, bool accept)
        {
            var application = await _applicationRepository.GetByIdForUpdateAsync(applicationId);
            if (application == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityApplicationNotFound, "Application not found");
            }

            var post = await _postRepository.GetByIdForUpdateAsync(application.PostId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.AuthorId != ownerId)
            {
                throw new ForbiddenException(ErrorCodes.CommunityPostNotOwned, "Only the post owner can respond to applications");
            }

            if (application.Status != CommunityApplicationStatuses.Pending)
            {
                throw new ConflictException(ErrorCodes.CommunityApplicationNotPending, "This application is no longer pending");
            }

            var now = DateTime.UtcNow;

            if (accept)
            {
                // Guarded by the post's optimistic concurrency Version: two concurrent accepts racing
                // for the last slot will have one succeed and the other raise a concurrency
                // conflict (409) at SaveChanges instead of overshooting MaxParticipants.
                if (post.MaxParticipants.HasValue && post.AcceptedParticipants >= post.MaxParticipants.Value)
                {
                    throw new ConflictException(ErrorCodes.CommunityPostFull, "This post is already full");
                }

                application.Status = CommunityApplicationStatuses.Accepted;
                application.RespondedAt = now;
                application.RespondedByUserId = ownerId;

                post.AcceptedParticipants++;
                if (post.MaxParticipants.HasValue && post.AcceptedParticipants >= post.MaxParticipants.Value)
                {
                    post.Status = CommunityPostStatuses.Closed;
                }
                post.Version++;
                post.UpdatedAt = now;
            }
            else
            {
                application.Status = CommunityApplicationStatuses.Rejected;
                application.RespondedAt = now;
                application.RespondedByUserId = ownerId;
            }

            await _postRepository.SaveChangesAsync();

            await NotifyAsync(
                application.ApplicantId,
                accept ? "Your join request was accepted" : "Your join request was declined",
                post.Title,
                NotificationTypeConstants.System);

            return Result<CommunityApplicationResponse>.Success(application.ToResponse());
        }

        private async Task<List<CommunityPostResponse>> MapListAsync(List<CommunityPost> items, Guid? currentUserId)
        {
            if (items.Count == 0 || !currentUserId.HasValue)
            {
                return items.Select(x => x.ToResponse(currentUserId, false, null)).ToList();
            }

            // Best-effort per-item lookups; feed sizes are page-bounded (<=100) so this stays cheap.
            var result = new List<CommunityPostResponse>(items.Count);
            foreach (var post in items)
            {
                var reacted = await _reactionRepository.GetAsync(post.Id, currentUserId.Value) != null;
                var applicationStatus = (await _applicationRepository.GetByPostAndApplicantAsync(post.Id, currentUserId.Value))?.Status;
                result.Add(post.ToResponse(currentUserId, reacted, applicationStatus));
            }

            return result;
        }

        private async Task EnsureActiveUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(ErrorCodes.UserNotFound, "User not found");
            }

            if (user.Status != UserStatuses.Active)
            {
                throw new ForbiddenException(ErrorCodes.AccountNotActive, "Your account is not active");
            }
        }

        private async Task NotifyAsync(Guid userId, string title, string content, string type)
        {
            var error = await _notificationRepository.TryAddAndSaveAsync(new[]
            {
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Content = content,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                }
            });

            // Best-effort: never fail the business operation over a notification failure.
            _ = error;
        }

        private static void AttachMedia(CommunityPost post, List<CommunityPostMediaRequest>? media)
        {
            if (media == null || media.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var index = 0;
            foreach (var m in media)
            {
                post.Media.Add(new CommunityPostMedia
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    MediaType = m.MediaType,
                    Url = m.Url,
                    ThumbnailUrl = m.ThumbnailUrl,
                    MimeType = m.MimeType,
                    FileSize = m.FileSize,
                    Width = m.Width,
                    Height = m.Height,
                    DurationSeconds = m.DurationSeconds,
                    OrderIndex = index++,
                    Status = CommunityMediaStatuses.Active,
                    CreatedAt = now
                });
            }
        }

        private static async Task ValidateOrThrowAsync<T>(IValidator<T> validator, T instance)
        {
            var result = await validator.ValidateAsync(instance);
            if (!result.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    result.Errors.Select(x => x.ErrorMessage).ToList());
            }
        }
    }
}
