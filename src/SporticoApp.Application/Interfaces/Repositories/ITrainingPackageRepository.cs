using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ITrainingPackageRepository
    {
        Task<TrainingPackage?> GetByIdAsync(Guid id);

        Task<TrainingPackage?> GetByIdWithCoachAsync(Guid id);

        Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedWithCoachAsync(
            TrainingPackageFilterRequest filter);

        Task<TrainingPackage?> GetByIdForUpdateAsync(Guid id);

        Task<TrainingPackage?> GetOwnedByIdAsync(Guid coachId, Guid id);

        Task<TrainingPackage?> GetOwnedByIdForUpdateAsync(Guid coachId, Guid id);

        Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedAsync(
            TrainingPackageFilterRequest filter);

        /// <summary>
        /// Loads the package's schedule slots tracked and ordered by session number, for capacity
        /// reservation/release during purchase. Empty list when the package has no schedule.
        /// </summary>
        Task<List<TrainingPackageSessionSlot>> GetSessionSlotsForUpdateAsync(Guid packageId);

        Task AddAsync(TrainingPackage trainingPackage);

        Task AddWithoutSaveAsync(TrainingPackage trainingPackage);

        Task SaveChangesAsync();
    }
}
