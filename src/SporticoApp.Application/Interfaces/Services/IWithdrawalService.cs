using SporticoApp.Application.DTOs.Withdrawals;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IWithdrawalService
    {
        Task<Result<WithdrawalRequestResponse>> CreateAsync(
            Guid coachId,
            CreateWithdrawalRequest request);

        Task<Result<PagedResult<WithdrawalRequestResponse>>> GetMyAsync(
            Guid coachId,
            WithdrawalRequestFilterRequest filter);

        Task<Result<PagedResult<WithdrawalRequestResponse>>> GetPendingAsync(
            WithdrawalRequestFilterRequest filter);

        Task<Result<WithdrawalRequestResponse>> ApproveAsync(
            Guid adminId,
            Guid id);

        Task<Result<WithdrawalRequestResponse>> RejectAsync(
            Guid adminId,
            Guid id,
            RejectWithdrawalRequest request);

        Task<Result<WithdrawalRequestResponse>> MarkPaidAsync(
            Guid adminId,
            Guid id);
    }
}
