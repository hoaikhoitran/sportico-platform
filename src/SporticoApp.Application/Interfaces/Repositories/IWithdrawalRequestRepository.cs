using SporticoApp.Application.DTOs.Withdrawals;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IWithdrawalRequestRepository
    {
        Task<WithdrawalRequest?> GetByIdAsync(Guid id);

        Task<WithdrawalRequest?> GetByIdForUpdateAsync(Guid id);

        Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPagedByCoachAsync(
            Guid coachId,
            WithdrawalRequestFilterRequest filter);

        Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPendingPagedAsync(
            WithdrawalRequestFilterRequest filter);

        Task AddAsync(WithdrawalRequest request);

        Task AddWithoutSaveAsync(WithdrawalRequest request);

        Task SaveChangesAsync();
    }
}
