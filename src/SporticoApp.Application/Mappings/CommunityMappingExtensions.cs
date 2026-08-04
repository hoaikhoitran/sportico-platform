using SporticoApp.Application.DTOs.Community;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Mappings
{
    public static class CommunityMappingExtensions
    {
        public static CommunityPostAuthorResponse ToAuthorResponse(this User user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl
        };

        public static CommunityPostResponse ToResponse(
            this CommunityPost post,
            Guid? currentUserId = null,
            bool currentUserReacted = false,
            string? currentUserApplicationStatus = null)
        {
            var isOwner = currentUserId.HasValue && post.AuthorId == currentUserId.Value;
            var isRecruitment = CommunityPostTypes.IsRecruitment(post.PostType);
            var slotsRemaining = post.MaxParticipants.HasValue
                ? Math.Max(0, post.MaxParticipants.Value - post.AcceptedParticipants)
                : (int?)null;

            var canApply =
                currentUserId.HasValue &&
                !isOwner &&
                isRecruitment &&
                post.Status == CommunityPostStatuses.Published &&
                (post.StartAt == null || post.StartAt > DateTime.UtcNow) &&
                (slotsRemaining == null || slotsRemaining > 0) &&
                currentUserApplicationStatus == null;

            return new CommunityPostResponse
            {
                Id = post.Id,
                Author = post.Author?.ToAuthorResponse() ?? new CommunityPostAuthorResponse { Id = post.AuthorId },
                SportId = post.SportId,
                SportName = post.Sport?.Name,
                PostType = post.PostType,
                Title = post.Title,
                Content = post.Content,
                LocationName = post.LocationName,
                Address = post.Address,
                Latitude = post.Latitude,
                Longitude = post.Longitude,
                StartAt = post.StartAt,
                EndAt = post.EndAt,
                MaxParticipants = post.MaxParticipants,
                AcceptedParticipants = post.AcceptedParticipants,
                SlotsRemaining = slotsRemaining,
                Level = post.Level,
                FeePerPerson = post.FeePerPerson,
                Status = post.Status,
                AllowComments = post.AllowComments,
                CommentCount = post.CommentCount,
                ReactionCount = post.ReactionCount,
                ApplicationCount = post.ApplicationCount,
                ViewCount = post.ViewCount,
                Media = post.Media
                    .OrderBy(m => m.OrderIndex)
                    .Select(m => new CommunityPostMediaResponse
                    {
                        Id = m.Id,
                        MediaType = m.MediaType,
                        Url = m.Url,
                        ThumbnailUrl = m.ThumbnailUrl,
                        OrderIndex = m.OrderIndex
                    })
                    .ToList(),
                PublishedAt = post.PublishedAt,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                CurrentUserReacted = currentUserReacted,
                CurrentUserApplicationStatus = currentUserApplicationStatus,
                CanApply = canApply,
                CanEdit = isOwner,
                CanModerate = false // set true explicitly by AdminCommunityService responses only
            };
        }

        public static AdminCommunityPostResponse ToAdminResponse(this CommunityPost post, int reportCount) => new()
        {
            Id = post.Id,
            Author = post.Author?.ToAuthorResponse() ?? new CommunityPostAuthorResponse { Id = post.AuthorId },
            PostType = post.PostType,
            Title = post.Title,
            Status = post.Status,
            ModerationReason = post.ModerationReason,
            ReportCount = reportCount,
            CommentCount = post.CommentCount,
            ReactionCount = post.ReactionCount,
            ApplicationCount = post.ApplicationCount,
            CreatedAt = post.CreatedAt,
            PublishedAt = post.PublishedAt,
            HiddenAt = post.HiddenAt,
            DeletedAt = post.DeletedAt
        };

        public static CommunityCommentResponse ToResponse(this CommunityComment comment, Guid? currentUserId, bool isPostOwner)
        {
            var isDeleted = comment.Status == CommunityCommentStatuses.Deleted;
            var isOwner = currentUserId.HasValue && comment.AuthorId == currentUserId.Value;

            return new CommunityCommentResponse
            {
                Id = comment.Id,
                PostId = comment.PostId,
                Author = comment.Author?.ToAuthorResponse() ?? new CommunityPostAuthorResponse { Id = comment.AuthorId },
                ParentCommentId = comment.ParentCommentId,
                Content = isDeleted ? "Bình luận đã bị xóa" : comment.Content,
                Status = comment.Status,
                ReplyCount = comment.ReplyCount,
                Replies = comment.Replies
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => r.ToResponse(currentUserId, isPostOwner))
                    .ToList(),
                CanEdit = !isDeleted && isOwner,
                CanModerate = isPostOwner,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }

        public static CommunityApplicationResponse ToResponse(this CommunityPostApplication app) => new()
        {
            Id = app.Id,
            PostId = app.PostId,
            Applicant = app.Applicant?.ToAuthorResponse() ?? new CommunityPostAuthorResponse { Id = app.ApplicantId },
            Message = app.Message,
            Status = app.Status,
            CreatedAt = app.CreatedAt,
            RespondedAt = app.RespondedAt,
            CancelledAt = app.CancelledAt
        };

        public static ReportResponse ToResponse(this Report report) => new()
        {
            Id = report.Id,
            ReporterId = report.reporter_id,
            TargetType = report.TargetType,
            TargetId = report.TargetId,
            Reason = report.Reason,
            Description = report.Description,
            Status = report.Status,
            HandledByUserId = report.HandledByUserId,
            HandledAt = report.HandledAt,
            ResolutionNote = report.ResolutionNote,
            ActionTaken = report.ActionTaken,
            CreatedAt = report.CreatedAt
        };
    }
}
