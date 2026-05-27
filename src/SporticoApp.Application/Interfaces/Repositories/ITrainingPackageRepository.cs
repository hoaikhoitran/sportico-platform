using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ITrainingPackageRepository
    {
        Task<TrainingPackage?> GetByIdAsync(Guid id);

        Task<TrainingPackage?> GetByIdForUpdateAsync(Guid id);

        Task<TrainingPackage?> GetOwnedByIdAsync(Guid coachId, Guid id);

        Task<TrainingPackage?> GetOwnedByIdForUpdateAsync(Guid coachId, Guid id);

        Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedAsync(
            TrainingPackageFilterRequest filter);

        Task AddAsync(TrainingPackage trainingPackage);

        Task AddWithoutSaveAsync(TrainingPackage trainingPackage);

        Task SaveChangesAsync();
    }
}
