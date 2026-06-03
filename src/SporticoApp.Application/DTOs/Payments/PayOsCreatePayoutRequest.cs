namespace SporticoApp.Application.DTOs.Payments
{
    public class PayOsCreatePayoutRequest
    {
        /// <summary>Internal reference; use WithdrawalRequest.Id.ToString().</summary>
        public string ReferenceId { get; set; } = string.Empty;

        /// <summary>Payout amount in VND (integer).</summary>
        public int Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        /// <summary>6-digit bank BIN of the destination bank.</summary>
        public string ToBin { get; set; } = string.Empty;

        public string ToAccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// Account holder name as registered with the bank (e.g. "NGUYEN VAN A").
        /// Sent as "toAccountName" in the PayOS Chi request body.
        /// Not included in the HMAC-SHA256 signature canonical string.
        /// Populate from CoachPayoutAccount.BankAccountHolder.
        /// </summary>
        public string? ToAccountName { get; set; }

        /// <summary>
        /// Optional PayOS payout category (e.g. "salary", "business").
        /// Leave null to omit from the request body entirely.
        /// Only set when the PayOS merchant account has this field configured.
        /// </summary>
        public string? Category { get; set; }
    }
}
