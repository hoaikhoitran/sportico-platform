using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class ReviewMappingExtensions
    {
        public static ReviewResponse ToResponse(this Review review, bool canEdit = false)
        {
            return new ReviewResponse
            {
                Id = review.Id,
                CoachId = review.CoachId,
                CoachName = review.Coach?.User?.FullName ?? string.Empty,
                LearnerId = review.learner_id,
                LearnerName = review.learner?.FullName ?? string.Empty,
                LearnerAvatarUrl = review.learner?.AvatarUrl,
                BookingId = review.BookingId,
                Rating = review.Rating,
                Comment = review.Comment,
                Status = review.Status,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
                CanEdit = canEdit
            };
        }

        public static ReviewReportResponse ToReviewReportResponse(this Report report, Review? review = null)
        {
            return new ReviewReportResponse
            {
                Id = report.Id,
                ReporterId = report.reporter_id,
                ReviewId = report.TargetId ?? Guid.Empty,
                Reason = report.Reason,
                Description = report.Description,
                Status = report.Status,
                ActionTaken = report.ActionTaken,
                HandledByUserId = report.HandledByUserId,
                HandledAt = report.HandledAt,
                ResolutionNote = report.ResolutionNote,
                CreatedAt = report.CreatedAt,
                ReviewRating = review?.Rating,
                ReviewComment = review?.Comment,
                ReviewStatus = review?.Status,
                ReviewCoachId = review?.CoachId,
                ReviewLearnerId = review?.learner_id
            };
        }
    }
}
