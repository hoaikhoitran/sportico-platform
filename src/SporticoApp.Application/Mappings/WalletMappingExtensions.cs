using SporticoApp.Application.DTOs.Wallets;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class WalletMappingExtensions
    {
        public static CoachWalletResponse ToResponse(this CoachWallet wallet)
        {
            return new CoachWalletResponse
            {
                Id = wallet.Id,
                CoachId = wallet.CoachId,
                AvailableBalance = wallet.AvailableBalance,
                PendingBalance = wallet.PendingBalance,
                TotalEarned = wallet.TotalEarned,
                TotalWithdrawn = wallet.TotalWithdrawn,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            };
        }

        public static CoachWalletTransactionResponse ToResponse(
            this CoachWalletTransaction transaction)
        {
            return new CoachWalletTransactionResponse
            {
                Id = transaction.Id,
                CoachWalletId = transaction.CoachWalletId,
                CoachId = transaction.CoachId,
                Type = transaction.Type,
                Direction = transaction.Direction,
                Amount = transaction.Amount,
                ReferenceType = transaction.ReferenceType,
                ReferenceId = transaction.ReferenceId,
                Note = transaction.Note,
                CreatedAt = transaction.CreatedAt
            };
        }
    }
}
