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

        Task<bool> HasOverlapAsync(
            Guid userId,
            DateTime startTime,
            DateTime endTime,
            List<string> activeStatuses);

        Task AddAsync(TrainingSession session);

        Task AddWithoutSaveAsync(TrainingSession session);

        Task SaveChangesAsync();
    }
}
