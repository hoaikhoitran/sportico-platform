using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IReviewService
    {
        Task<Result<ReviewResponse>> CreateAsync(Guid learnerId, CreateReviewRequest request);

        Task<Result<ReviewResponse>> UpdateAsync(Guid learnerId, Guid reviewId, UpdateReviewRequest request);

        Task<Result<object>> DeleteOwnAsync(Guid learnerId, Guid reviewId);

        Task<Result<PagedResult<ReviewResponse>>> GetCoachReviewsAsync(
            Guid? currentUserId,
            Guid coachId,
            ReviewFilterRequest filter);

        Task<Result<CoachReviewSummaryResponse>> GetCoachReviewSummaryAsync(Guid coachId);

        Task<Result<ReviewResponse>> GetMyReviewForCoachAsync(Guid learnerId, Guid coachId);
    }
}
