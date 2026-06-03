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
        /// Optional PayOS payout category (e.g. "salary", "business").
        /// Sent as a JSON string array ["salary"] per PayOS Chi API spec.
        /// Leave null to omit from the request body entirely.
        /// Only set when the PayOS merchant account has this field configured.
        /// </summary>
        public string? Category { get; set; }
    }
}
