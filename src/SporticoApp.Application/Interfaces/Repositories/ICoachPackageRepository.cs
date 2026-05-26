using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICoachPackageRepository
    {
        Task<CoachPackage?> GetCurrentByCoachIdAsync(Guid coachId);

        Task<CoachPackage?> GetCurrentForUpdateAsync(Guid coachId);

        Task<(List<CoachPackage> Items, int TotalCount)> GetHistoryAsync(
            Guid coachId,
            int pageNumber,
            int pageSize);

        Task AddAsync(CoachPackage coachPackage);

        Task AddWithoutSaveAsync(CoachPackage coachPackage);

        Task SaveChangesAsync();
    }
}