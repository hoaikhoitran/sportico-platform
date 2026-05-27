namespace SporticoApp.Application.DTOs.PayoutAccounts
{
    public class CoachPayoutAccountResponse
    {
        public Guid Id { get; set; }

        public Guid CoachId { get; set; }

        public string PayoutMethod { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        public string BankAccountNumber { get; set; } = string.Empty;

        public string BankAccountHolder { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
