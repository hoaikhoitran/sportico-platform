using SporticoApp.Application.DTOs.Withdrawals;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class WithdrawalMappingExtensions
    {
        public static WithdrawalRequestResponse ToResponse(this WithdrawalRequest request)
        {
            return new WithdrawalRequestResponse
            {
                Id = request.Id,
                CoachId = request.CoachId,
                CoachWalletId = request.CoachWalletId,
                CoachPayoutAccountId = request.CoachPayoutAccountId,
                Amount = request.Amount,
                Status = request.Status,
                AdminNote = request.AdminNote,
                ReviewedByUserId = request.ReviewedByUserId,
                ReviewedAt = request.ReviewedAt,
                PayOsPayoutId = request.PayOsPayoutId,
                PayOsReferenceId = request.PayOsReferenceId,
                PayOsPayoutStatus = request.PayOsPayoutStatus,
                FailureReason = request.FailureReason,
                ProcessingAt = request.ProcessingAt,
                PaidAt = request.PaidAt,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }

        public static string MaskAccountNumber(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length <= 4)
                return accountNumber;

            var last4 = accountNumber[^4..];
            return $"******{last4}";
        }
    }
}
