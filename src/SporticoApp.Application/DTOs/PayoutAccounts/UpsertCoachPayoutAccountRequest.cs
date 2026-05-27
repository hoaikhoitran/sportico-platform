namespace SporticoApp.Application.DTOs.PayoutAccounts
{
    public class UpsertCoachPayoutAccountRequest
    {
        public string PayoutMethod { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        public string BankAccountNumber { get; set; } = string.Empty;

        public string BankAccountHolder { get; set; } = string.Empty;
    }
}
