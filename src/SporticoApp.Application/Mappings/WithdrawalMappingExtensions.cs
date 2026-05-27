using SporticoApp.Application.DTOs.Withdrawals;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class WithdrawalMappingExtensions
    {
        public static WithdrawalRequestResponse ToResponse(
            this WithdrawalRequest request)
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
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }
    }
}
