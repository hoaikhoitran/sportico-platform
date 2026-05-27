namespace SporticoApp.Application.DTOs.Wallets
{
    public class CoachWalletResponse
    {
        public Guid Id { get; set; }

        public Guid CoachId { get; set; }

        public decimal AvailableBalance { get; set; }

        public decimal PendingBalance { get; set; }

        public decimal TotalEarned { get; set; }

        public decimal TotalWithdrawn { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
