using SporticoApp.Application.DTOs.TrainingSessions;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ITrainingSessionRepository
    {
        Task<TrainingSession?> GetByIdAsync(Guid id);

        Task<TrainingSession?> GetByIdForUpdateAsync(Guid id);

        Task<(List<TrainingSession> Items, int TotalCount)> GetByBookingPagedAsync(
            Guid bookingId,
            TrainingSessionFilterRequest filter);

        Task<int> CountByBookingAsync(Guid bookingId, List<string> statuses);

        /// <summary>
        /// Counts active (capacity-occupying) sessions on one availability slot. Optionally excludes a
        /// session id (used during cancellation to count the OTHER active sessions).
        /// </summary>
        Task<int> CountActiveByAvailabilitySlotIdAsync(
            Guid slotId,
            IEnumerable<string> statuses,
            Guid? excludeSessionId = null);

        /// <summary>
        /// Batch variant: active session counts keyed by availability slot id (slots with zero active
        /// sessions are absent from the dictionary). Avoids N+1 when mapping a page of slots.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, int>> CountActiveByAvailabilitySlotIdsAsync(
            IReadOnlyCollection<Guid> slotIds,
            IEnumerable<string> statuses);

        Task<bool> HasOverlapAsync(
            Guid userId,
            DateTime startTime,
            DateTime endTime,
            List<string> activeStatuses);

        Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByLearnerAsync(
            Guid learnerId,
            TrainingSessionFilterRequest filter);

        Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByCoachAsync(
            Guid coachId,
            TrainingSessionFilterRequest filter);

        Task AddAsync(TrainingSession session);

        Task AddWithoutSaveAsync(TrainingSession session);

        Task SaveChangesAsync();
    }
}
