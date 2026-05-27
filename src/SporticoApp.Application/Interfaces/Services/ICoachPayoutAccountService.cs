using SporticoApp.Application.DTOs.PayoutAccounts;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICoachPayoutAccountService
    {
        Task<Result<CoachPayoutAccountResponse>> GetMyAsync(Guid coachId);

        Task<Result<CoachPayoutAccountResponse>> UpsertAsync(
            Guid coachId,
            UpsertCoachPayoutAccountRequest request);

        Task<Result<PagedResult<CoachPayoutAccountResponse>>> GetPendingAsync(
            int pageNumber,
            int pageSize);

        Task<Result<CoachPayoutAccountResponse>> VerifyAsync(
            Guid adminId,
            Guid id);

        Task<Result<CoachPayoutAccountResponse>> RejectAsync(
            Guid adminId,
            Guid id,
            RejectCoachPayoutAccountRequest request);
    }
}
