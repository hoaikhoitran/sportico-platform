namespace SporticoApp.Application.DTOs.Wallets
{
    public class CoachWalletTransactionResponse
    {
        public Guid Id { get; set; }

        public Guid CoachWalletId { get; set; }

        public Guid CoachId { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Direction { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string? ReferenceType { get; set; }

        public Guid? ReferenceId { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
