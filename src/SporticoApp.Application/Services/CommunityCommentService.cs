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

    public class CommunityCommentService : ICommunityCommentService
    {
        private readonly ICommunityCommentRepository _commentRepository;
        private readonly ICommunityPostRepository _postRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<CommunityCommentFilterRequest> _filterValidator;
        private readonly IValidator<CreateCommentRequest> _createValidator;
        private readonly IValidator<CreateReplyRequest> _replyValidator;
        private readonly IValidator<UpdateCommentRequest> _updateValidator;

        public CommunityCommentService(
            ICommunityCommentRepository commentRepository,
            ICommunityPostRepository postRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IValidator<CommunityCommentFilterRequest> filterValidator,
            IValidator<CreateCommentRequest> createValidator,
            IValidator<CreateReplyRequest> replyValidator,
            IValidator<UpdateCommentRequest> updateValidator)
        {
            _commentRepository = commentRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _filterValidator = filterValidator;
            _createValidator = createValidator;
            _replyValidator = replyValidator;
            _updateValidator = updateValidator;
        }

        public async Task<Result<PagedResult<CommunityCommentResponse>>> GetCommentsAsync(
            Guid? currentUserId, Guid postId, CommunityCommentFilterRequest filter)
        {
            await ValidateOrThrowAsync(_filterValidator, filter);

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null || post.Status == CommunityPostStatuses.Deleted || post.Status == CommunityPostStatuses.Hidden)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            var (items, totalCount) = await _commentRepository.GetRootCommentsPagedAsync(postId, filter);

            return Result<PagedResult<CommunityCommentResponse>>.Success(new PagedResult<CommunityCommentResponse>(
                items.Select(x => x.ToResponse(currentUserId, false)).ToList(), totalCount, filter.PageNumber, filter.PageSize));
        }

        public async Task<Result<CommunityCommentResponse>> AddCommentAsync(Guid userId, Guid postId, CreateCommentRequest request)
        {
            await ValidateOrThrowAsync(_createValidator, request);
            await EnsureActiveUserAsync(userId);

            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null || post.Status == CommunityPostStatuses.Deleted)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (!post.AllowComments)
            {
                throw new ConflictException(ErrorCodes.CommunityCommentsDisabled, "Comments are disabled on this post");
            }

            var now = DateTime.UtcNow;
            var comment = new CommunityComment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                AuthorId = userId,
                ParentCommentId = null,
                Content = request.Content.Trim(),
                Status = CommunityCommentStatuses.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _commentRepository.AddWithoutSaveAsync(comment);

            post.CommentCount++;
            post.UpdatedAt = now;

            await _postRepository.SaveChangesAsync();

            if (post.AuthorId != userId)
            {
                await NotifyAsync(post.AuthorId, "New comment", $"Someone commented on \"{post.Title}\"");
            }

            var author = await _userRepository.GetByIdAsync(userId);
            comment.Author = author!;

            return Result<CommunityCommentResponse>.Success(comment.ToResponse(userId, false));
        }

        public async Task<Result<CommunityCommentResponse>> AddReplyAsync(Guid userId, Guid parentCommentId, CreateReplyRequest request)
        {
            await ValidateOrThrowAsync(_replyValidator, request);
            await EnsureActiveUserAsync(userId);

            var parent = await _commentRepository.GetByIdForUpdateAsync(parentCommentId);
            if (parent == null || parent.Status == CommunityCommentStatuses.Deleted)
            {
                throw new NotFoundException(ErrorCodes.CommunityCommentNotFound, "Comment not found");
            }

            // Only one level of nesting: a reply cannot itself be replied to.
            if (parent.ParentCommentId != null)
            {
                throw new ConflictException(
                    ErrorCodes.CommunityCommentNestingNotAllowed,
                    "Cannot reply to a reply — reply to the original comment instead");
            }

            var post = await _postRepository.GetByIdForUpdateAsync(parent.PostId);
            if (post == null || post.Status == CommunityPostStatuses.Deleted)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (!post.AllowComments)
            {
                throw new ConflictException(ErrorCodes.CommunityCommentsDisabled, "Comments are disabled on this post");
            }

            var now = DateTime.UtcNow;
            var reply = new CommunityComment
            {
                Id = Guid.NewGuid(),
                PostId = parent.PostId,
                AuthorId = userId,
                ParentCommentId = parent.Id,
                Content = request.Content.Trim(),
                Status = CommunityCommentStatuses.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _commentRepository.AddWithoutSaveAsync(reply);

            parent.ReplyCount++;
            parent.UpdatedAt = now;
            post.CommentCount++;
            post.UpdatedAt = now;

            await _postRepository.SaveChangesAsync();

            if (parent.AuthorId != userId)
            {
                await NotifyAsync(parent.AuthorId, "New reply", "Someone replied to your comment");
            }

            var author = await _userRepository.GetByIdAsync(userId);
            reply.Author = author!;

            return Result<CommunityCommentResponse>.Success(reply.ToResponse(userId, false));
        }

        public async Task<Result<CommunityCommentResponse>> UpdateCommentAsync(Guid userId, Guid commentId, UpdateCommentRequest request)
        {
            await ValidateOrThrowAsync(_updateValidator, request);

            var comment = await _commentRepository.GetByIdForUpdateAsync(commentId);
            if (comment == null || comment.Status == CommunityCommentStatuses.Deleted)
            {
                throw new NotFoundException(ErrorCodes.CommunityCommentNotFound, "Comment not found");
            }

            if (comment.AuthorId != userId)
            {
                throw new ForbiddenException(ErrorCodes.CommunityCommentNotOwned, "This comment does not belong to you");
            }

            comment.Content = request.Content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;

            await _commentRepository.SaveChangesAsync();

            var author = await _userRepository.GetByIdAsync(userId);
            comment.Author = author!;

            return Result<CommunityCommentResponse>.Success(comment.ToResponse(userId, false));
        }

        public async Task<Result<object>> DeleteCommentAsync(Guid userId, Guid commentId)
        {
            var comment = await _commentRepository.GetByIdForUpdateAsync(commentId);
            if (comment == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityCommentNotFound, "Comment not found");
            }

            if (comment.AuthorId != userId)
            {
                throw new ForbiddenException(ErrorCodes.CommunityCommentNotOwned, "This comment does not belong to you");
            }

            if (comment.Status == CommunityCommentStatuses.Deleted)
            {
                return Result<object>.Success(new { deleted = true }); // idempotent
            }

            comment.Status = CommunityCommentStatuses.Deleted;
            comment.DeletedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;

            var post = await _postRepository.GetByIdForUpdateAsync(comment.PostId);
            if (post != null)
            {
                post.CommentCount = Math.Max(0, post.CommentCount - 1);
                post.UpdatedAt = DateTime.UtcNow;
            }

            await _commentRepository.SaveChangesAsync();

            return Result<object>.Success(new { deleted = true });
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

        private async Task NotifyAsync(Guid userId, string title, string content)
        {
            await _notificationRepository.TryAddAndSaveAsync(new[]
            {
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Content = content,
                    Type = NotificationTypeConstants.Post,
                    CreatedAt = DateTime.UtcNow
                }
            });
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
