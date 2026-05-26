using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IPackageRepository
    {
        Task<bool> ExistsByNameAsync(string name);

        Task<bool> ExistsByNameExceptIdAsync(string name, int excludedId);

        Task<Package?> GetByIdAsync(int id);

        Task<Package?> GetForUpdateByIdAsync(int id);

        Task<Package?> GetActiveByIdAsync(int id);

        Task<(List<Package> Items, int TotalCount)> GetPagedAsync(
            string? keyword,
            bool? isActive,
            int pageNumber,
            int pageSize);

        Task AddAsync(Package package);

        Task SaveChangesAsync();
    }
}
