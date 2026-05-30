namespace SporticoApp.Application.DTOs.PayoutAccounts
{
    public class UpsertCoachPayoutAccountRequest
    {
        public string PayoutMethod { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        /// <summary>6-digit bank BIN (e.g. "970415" for VietinBank).</summary>
        public string BankBin { get; set; } = string.Empty;

        public string BankAccountNumber { get; set; } = string.Empty;

        public string BankAccountHolder { get; set; } = string.Empty;
    }
}
