using FluentValidation;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class AdminCommunityService : IAdminCommunityService
    {
        private readonly ICommunityPostRepository _postRepository;
        private readonly ICommunityCommentRepository _commentRepository;
        private readonly ICommunityReportRepository _reportRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<AdminCommunityPostFilterRequest> _postFilterValidator;
        private readonly IValidator<CommunityCommentFilterRequest> _commentFilterValidator;
        private readonly IValidator<HideContentRequest> _hideValidator;
        private readonly IValidator<ResolveReportRequest> _resolveValidator;

        public AdminCommunityService(
            ICommunityPostRepository postRepository,
            ICommunityCommentRepository commentRepository,
            ICommunityReportRepository reportRepository,
            INotificationRepository notificationRepository,
            IValidator<AdminCommunityPostFilterRequest> postFilterValidator,
            IValidator<CommunityCommentFilterRequest> commentFilterValidator,
            IValidator<HideContentRequest> hideValidator,
            IValidator<ResolveReportRequest> resolveValidator)
        {
            _postRepository = postRepository;
            _commentRepository = commentRepository;
            _reportRepository = reportRepository;
            _notificationRepository = notificationRepository;
            _postFilterValidator = postFilterValidator;
            _commentFilterValidator = commentFilterValidator;
            _hideValidator = hideValidator;
            _resolveValidator = resolveValidator;
        }

        public async Task<Result<PagedResult<AdminCommunityPostResponse>>> GetPostsAsync(AdminCommunityPostFilterRequest filter)
        {
            await ValidateOrThrowAsync(_postFilterValidator, filter);

            var (items, totalCount) = await _postRepository.GetPagedForAdminAsync(filter);

            var responses = new List<AdminCommunityPostResponse>(items.Count);
            foreach (var post in items)
            {
                var reportCount = await _reportRepository.CountOpenByTargetAsync(ReportTargetTypes.CommunityPost, post.Id);
                responses.Add(post.ToAdminResponse(reportCount));
            }

            return Result<PagedResult<AdminCommunityPostResponse>>.Success(
                new PagedResult<AdminCommunityPostResponse>(responses, totalCount, filter.PageNumber, filter.PageSize));
        }

        public async Task<Result<CommunityPostResponse>> GetPostByIdAsync(Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            var response = post.ToResponse();
            response.CanModerate = true;
            return Result<CommunityPostResponse>.Success(response);
        }

        public async Task<Result<CommunityPostResponse>> HidePostAsync(Guid adminId, Guid postId, HideContentRequest request)
        {
            await ValidateOrThrowAsync(_hideValidator, request);

            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.Status != CommunityPostStatuses.Hidden)
            {
                post.Status = CommunityPostStatuses.Hidden;
                post.HiddenByUserId = adminId;
                post.HiddenAt = DateTime.UtcNow;
                post.ModerationReason = request.Reason;
                post.UpdatedAt = DateTime.UtcNow;

                await _postRepository.SaveChangesAsync();

                await NotifyAsync(post.AuthorId, "Your post was hidden", request.Reason);
            }

            var response = post.ToResponse();
            response.CanModerate = true;
            return Result<CommunityPostResponse>.Success(response);
        }

        public async Task<Result<CommunityPostResponse>> RestorePostAsync(Guid adminId, Guid postId)
        {
            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.Status == CommunityPostStatuses.Hidden || post.Status == CommunityPostStatuses.Deleted)
            {
                post.Status = post.PublishedAt.HasValue ? CommunityPostStatuses.Published : CommunityPostStatuses.Draft;
                post.HiddenByUserId = null;
                post.HiddenAt = null;
                post.ModerationReason = null;
                post.DeletedAt = null;
                post.UpdatedAt = DateTime.UtcNow;

                await _postRepository.SaveChangesAsync();
            }

            var response = post.ToResponse();
            response.CanModerate = true;
            return Result<CommunityPostResponse>.Success(response);
        }

        public async Task<Result<object>> DeletePostAsync(Guid adminId, Guid postId)
        {
            var post = await _postRepository.GetByIdForUpdateAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
            }

            if (post.Status != CommunityPostStatuses.Deleted)
            {
                post.Status = CommunityPostStatuses.Deleted;
                post.DeletedAt = DateTime.UtcNow;
                post.HiddenByUserId ??= adminId;
                post.UpdatedAt = DateTime.UtcNow;

                await _postRepository.SaveChangesAsync();

                await NotifyAsync(post.AuthorId, "Your post was removed", "Your post violated community guidelines and was removed by an admin.");
            }

            return Result<object>.Success(new { deleted = true });
        }

        public async Task<Result<PagedResult<CommunityCommentResponse>>> GetCommentsAsync(Guid postId, CommunityCommentFilterRequest filter)
        {
            await ValidateOrThrowAsync(_commentFilterValidator, filter);

            var (items, totalCount) = await _commentRepository.GetForAdminPagedAsync(postId, filter);

            var responses = items.Select(x =>
            {
                var r = x.ToResponse(null, true);
                r.CanEdit = false;
                return r;
            }).ToList();

            return Result<PagedResult<CommunityCommentResponse>>.Success(
                new PagedResult<CommunityCommentResponse>(responses, totalCount, filter.PageNumber, filter.PageSize));
        }

        public async Task<Result<CommunityCommentResponse>> HideCommentAsync(Guid adminId, Guid commentId, HideContentRequest request)
        {
            await ValidateOrThrowAsync(_hideValidator, request);

            var comment = await _commentRepository.GetByIdForUpdateAsync(commentId);
            if (comment == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityCommentNotFound, "Comment not found");
            }

            if (comment.Status != CommunityCommentStatuses.Hidden)
            {
                comment.Status = CommunityCommentStatuses.Hidden;
                comment.HiddenByUserId = adminId;
                comment.HiddenAt = DateTime.UtcNow;
                comment.ModerationReason = request.Reason;
                comment.UpdatedAt = DateTime.UtcNow;

                await _commentRepository.SaveChangesAsync();

                await NotifyAsync(comment.AuthorId, "Your comment was hidden", request.Reason);
            }

            var response = comment.ToResponse(null, true);
            return Result<CommunityCommentResponse>.Success(response);
        }

        public async Task<Result<CommunityCommentResponse>> RestoreCommentAsync(Guid adminId, Guid commentId)
        {
            var comment = await _commentRepository.GetByIdForUpdateAsync(commentId);
            if (comment == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityCommentNotFound, "Comment not found");
            }

            if (comment.Status == CommunityCommentStatuses.Hidden || comment.Status == CommunityCommentStatuses.Deleted)
            {
                comment.Status = CommunityCommentStatuses.Active;
                comment.HiddenByUserId = null;
                comment.HiddenAt = null;
                comment.ModerationReason = null;
                comment.DeletedAt = null;
                comment.UpdatedAt = DateTime.UtcNow;

                await _commentRepository.SaveChangesAsync();
            }

            var response = comment.ToResponse(null, true);
            return Result<CommunityCommentResponse>.Success(response);
        }

        public async Task<Result<object>> DeleteCommentAsync(Guid adminId, Guid commentId)
        {
            var comment = await _commentRepository.GetByIdForUpdateAsync(commentId);
            if (comment == null)
            {
                throw new NotFoundException(ErrorCodes.CommunityCommentNotFound, "Comment not found");
            }

            if (comment.Status != CommunityCommentStatuses.Deleted)
            {
                comment.Status = CommunityCommentStatuses.Deleted;
                comment.DeletedAt = DateTime.UtcNow;
                comment.HiddenByUserId ??= adminId;
                comment.UpdatedAt = DateTime.UtcNow;

                await _commentRepository.SaveChangesAsync();
            }

            return Result<object>.Success(new { deleted = true });
        }

        public async Task<Result<PagedResult<ReportResponse>>> GetReportsAsync(AdminReportFilterRequest filter)
        {
            var (items, totalCount) = await _reportRepository.GetPagedAsync(filter);

            return Result<PagedResult<ReportResponse>>.Success(new PagedResult<ReportResponse>(
                items.Select(x => x.ToResponse()).ToList(), totalCount, filter.PageNumber, filter.PageSize));
        }

        public async Task<Result<ReportResponse>> ResolveReportAsync(Guid adminId, Guid reportId, ResolveReportRequest request)
        {
            await ValidateOrThrowAsync(_resolveValidator, request);

            var report = await _reportRepository.GetByIdForUpdateAsync(reportId);
            if (report == null)
            {
                throw new NotFoundException(ErrorCodes.ReportNotFound, "Report not found");
            }

            if (report.Status == ReportStatuses.Resolved || report.Status == ReportStatuses.Rejected)
            {
                throw new ConflictException(ErrorCodes.ReportNotFound, "This report has already been handled");
            }

            report.Status = request.Status;
            report.HandledByUserId = adminId;
            report.HandledAt = DateTime.UtcNow;
            report.ResolutionNote = request.ResolutionNote;
            report.ActionTaken = request.ActionTaken;

            await _reportRepository.SaveChangesAsync();

            // Apply the moderation action the admin chose, reusing the exact same hide/delete paths.
            if (report.TargetId.HasValue)
            {
                if (request.ActionTaken == ReportActions.PostHidden)
                {
                    await HidePostAsync(adminId, report.TargetId.Value, new HideContentRequest { Reason = request.ResolutionNote ?? report.Reason });
                }
                else if (request.ActionTaken == ReportActions.PostDeleted)
                {
                    await DeletePostAsync(adminId, report.TargetId.Value);
                }
                else if (request.ActionTaken == ReportActions.CommentHidden)
                {
                    await HideCommentAsync(adminId, report.TargetId.Value, new HideContentRequest { Reason = request.ResolutionNote ?? report.Reason });
                }
                else if (request.ActionTaken == ReportActions.CommentDeleted)
                {
                    await DeleteCommentAsync(adminId, report.TargetId.Value);
                }
            }

            return Result<ReportResponse>.Success(report.ToResponse());
        }

        private async Task NotifyAsync(Guid userId, string title, string? content)
        {
            await _notificationRepository.TryAddAndSaveAsync(new[]
            {
                new SporticoApp.Core.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Content = content,
                    Type = NotificationTypeConstants.Report,
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
