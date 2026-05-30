namespace SporticoApp.Application.DTOs.Payments
{
    /// <summary>The data payload inside a PayOS payout API response.</summary>
    public class PayOsPayoutData
    {
        public string Id { get; set; } = string.Empty;

        public string ReferenceId { get; set; } = string.Empty;

        /// <summary>PayOS payout state: PROCESSING | SUCCESS | FAILED | CANCELLED</summary>
        public string State { get; set; } = string.Empty;

        public string? ToBin { get; set; }

        public string? ToAccountNumber { get; set; }

        public string? ToAccountName { get; set; }

        public int? Amount { get; set; }

        public string? Description { get; set; }

        public string? Category { get; set; }
    }
}
