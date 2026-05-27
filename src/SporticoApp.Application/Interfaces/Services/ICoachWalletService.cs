using SporticoApp.Application.DTOs.Wallets;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICoachWalletService
    {
        Task<Result<CoachWalletResponse>> GetMyWalletAsync(Guid coachId);

        Task<Result<PagedResult<CoachWalletTransactionResponse>>> GetMyTransactionsAsync(
            Guid coachId,
            CoachWalletTransactionFilterRequest filter);
    }
}
