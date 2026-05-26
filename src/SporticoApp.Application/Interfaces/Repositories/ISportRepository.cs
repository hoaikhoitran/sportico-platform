using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories;

public interface ISportRepository
{
    Task<List<int>> GetActiveSportIdsAsync(List<int> sportIds);

    Task<bool> ExistsByNameAsync(string name);

    Task<bool> ExistsBySlugAsync(string slug);

    Task<Sport?> GetByIdAsync(int id);

    Task AddAsync(Sport sport);
}