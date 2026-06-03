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

        /// <summary>Coach: get one of their own withdrawal requests (ownership enforced).</summary>
        Task<Result<WithdrawalRequestResponse>> GetMyByIdAsync(Guid coachId, Guid id);

        Task<Result<PagedResult<WithdrawalRequestResponse>>> GetPendingAsync(
            WithdrawalRequestFilterRequest filter);

        /// <summary>Admin: list all withdrawal requests, optionally filtered by any status.</summary>
        Task<Result<PagedResult<WithdrawalRequestResponse>>> GetAllAsync(
            WithdrawalRequestFilterRequest filter);

        /// <summary>Admin: get a single withdrawal request by id (for review modals).</summary>
        Task<Result<WithdrawalRequestResponse>> GetByIdAdminAsync(Guid id);

        Task<Result<WithdrawalRequestResponse>> ApproveAsync(Guid adminId, Guid id);

        Task<Result<WithdrawalRequestResponse>> RejectAsync(
            Guid adminId,
            Guid id,
            RejectWithdrawalRequest request);

        /// <summary>Admin manual mark-paid fallback. Blocked if PayOS payout is still processing.</summary>
        Task<Result<WithdrawalRequestResponse>> MarkPaidAsync(Guid adminId, Guid id);

        /// <summary>Admin: query PayOS for the latest payout state and update the withdrawal.</summary>
        Task<Result<WithdrawalRequestResponse>> RefreshPayoutStatusAsync(Guid id);

        /// <summary>Admin: retry a failed payout with a new idempotency key.</summary>
        Task<Result<WithdrawalRequestResponse>> RetryPayoutAsync(Guid id);

        Task<Result<WithdrawalReceiptResponse>> GetReceiptAsync(Guid coachId, Guid id);

        Task<Result<WithdrawalReceiptResponse>> GetReceiptAdminAsync(Guid id);

        /// <summary>
        /// Admin diagnostic: returns safe payout account metadata for a withdrawal to help debug
        /// PayOS Chi rejections. Shows raw vs normalised account holder, BIN validity, etc.
        /// Never returns credentials or full account numbers.
        /// </summary>
        Task<Result<PayoutAccountDebugResponse>> GetPayoutAccountDebugAsync(Guid id);
    }
}
