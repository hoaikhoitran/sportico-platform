using SporticoApp.Application.DTOs.Payments;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IPayOsPayoutService
    {
        /// <summary>Returns the current payout-account balance from PayOS.</summary>
        Task<PayOsPayoutBalanceResponse> GetBalanceAsync();

        /// <summary>
        /// Submits a single payout to PayOS.
        /// <paramref name="idempotencyKey"/> must be unique per attempt;
        /// use WithdrawalRequest.Id for the first attempt.
        /// </summary>
        Task<PayOsCreatePayoutResponse> CreatePayoutAsync(
            PayOsCreatePayoutRequest request,
            string idempotencyKey);

        /// <summary>Queries the current state of a previously created payout.</summary>
        Task<PayOsPayoutDetailResponse> GetPayoutDetailAsync(string payoutId);
    }
}
