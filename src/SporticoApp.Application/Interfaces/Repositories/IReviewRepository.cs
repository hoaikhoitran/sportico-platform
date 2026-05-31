using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IReviewRepository
    {
        Task<Review?> GetByIdAsync(Guid id);

        Task<Review?> GetByIdForUpdateAsync(Guid id);

        Task<Review?> GetByCoachAndLearnerAsync(Guid coachId, Guid learnerId);

        Task<Review?> GetByCoachAndLearnerForUpdateAsync(Guid coachId, Guid learnerId);

        Task<(List<Review> Items, int TotalCount)> GetPagedByCoachAsync(
            Guid coachId,
            ReviewFilterRequest filter);

        Task AddAsync(Review review);

        Task SaveChangesAsync();

        /// <summary>Active-review rating breakdown + average for a coach.</summary>
        Task<CoachRatingStats> GetRatingStatsByCoachAsync(Guid coachId);

        /// <summary>
        /// True when the learner has a paid (active|completed, PaidAt set) booking with the coach.
        /// When <paramref name="bookingId"/> is given, that specific booking must match and qualify.
        /// </summary>
        Task<bool> HasSuccessfulBookingForReviewAsync(Guid learnerId, Guid coachId, Guid? bookingId);

        /// <summary>As above, but additionally the booking must be non-expired (ExpiresAt null or future).</summary>
        Task<bool> HasNonExpiredSuccessfulBookingAsync(Guid learnerId, Guid coachId);

        /// <summary>Recomputes and persists CoachProfile.Rating and TotalReviews from active reviews.</summary>
        Task RecalculateCoachRatingAsync(Guid coachId);
    }
}
