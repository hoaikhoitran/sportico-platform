using SporticoApp.Application.DTOs.Wallets;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICoachWalletRepository
    {
        Task<CoachWallet?> GetByCoachIdAsync(Guid coachId);

        Task<CoachWallet?> GetByCoachIdForUpdateAsync(Guid coachId);

        Task<(List<CoachWalletTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(
            Guid coachId,
            CoachWalletTransactionFilterRequest filter);

        Task AddAsync(CoachWallet wallet);

        Task AddWithoutSaveAsync(CoachWallet wallet);

        Task AddTransactionWithoutSaveAsync(CoachWalletTransaction transaction);

        Task SaveChangesAsync();
    }
}
