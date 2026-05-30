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

        /// <summary>PayOS payout category, e.g. "salary".</summary>
        public string? Category { get; set; }
    }
}
