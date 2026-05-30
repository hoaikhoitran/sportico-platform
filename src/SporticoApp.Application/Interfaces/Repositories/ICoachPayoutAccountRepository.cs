using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICoachPayoutAccountRepository
    {
        Task<CoachPayoutAccount?> GetByCoachIdAsync(Guid coachId);

        Task<CoachPayoutAccount?> GetByCoachIdForUpdateAsync(Guid coachId);

        Task<CoachPayoutAccount?> GetByIdAsync(Guid id);

        Task<CoachPayoutAccount?> GetByIdForUpdateAsync(Guid id);

        Task<(List<CoachPayoutAccount> Items, int TotalCount)> GetPendingPagedAsync(
            int pageNumber,
            int pageSize);

        Task AddAsync(CoachPayoutAccount account);

        Task SaveChangesAsync();
    }
}
