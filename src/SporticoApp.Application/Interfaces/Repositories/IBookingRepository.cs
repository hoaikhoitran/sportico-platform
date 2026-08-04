using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(Guid id);

        Task<Booking?> GetByIdForUpdateAsync(Guid id);

        Task<Booking?> GetByIdWithTrainingPackageAsync(Guid id);

        Task<Booking?> GetByIdForLearnerAsync(Guid learnerId, Guid id);

        Task<Booking?> GetByIdForCoachAsync(Guid coachId, Guid id);

        Task<Booking?> GetByIdForLearnerForUpdateAsync(Guid learnerId, Guid id);

        Task<Booking?> GetByIdForCoachForUpdateAsync(Guid coachId, Guid id);

        Task<(List<Booking> Items, int TotalCount)> GetPagedByLearnerAsync(
            Guid learnerId,
            BookingFilterRequest filter);

        Task<(List<Booking> Items, int TotalCount)> GetPagedByCoachAsync(
            Guid coachId,
            BookingFilterRequest filter);

        Task<(List<Booking> Items, int TotalCount)> GetPagedAsync(
            BookingFilterRequest filter);

        Task<Booking?> GetActiveOrCompletedBetweenUsersAsync(
            Guid learnerId,
            Guid coachId);

        /// <summary>
        /// Ids of PendingPayment bookings whose PayOS payment has an ExpiredAt in the past and is
        /// still Pending (never resolved by webhook or reconcile) — candidates for the expiry sweep.
        /// </summary>
        Task<List<Guid>> GetExpiredPendingPaymentBookingIdsAsync(DateTime nowUtc, int batchSize);

        Task AddAsync(Booking booking);

        Task AddWithoutSaveAsync(Booking booking);

        Task SaveChangesAsync();
    }
}
