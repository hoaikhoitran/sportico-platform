namespace SporticoApp.Application.DTOs.Withdrawals
{
    /// <summary>
    /// Withdrawal payout receipt.
    /// Generated dynamically from WithdrawalRequest + CoachPayoutAccount + User records.
    /// Bank account number is masked for privacy.
    /// </summary>
    public class WithdrawalReceiptResponse
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public Guid WithdrawalRequestId { get; set; }
        public Guid CoachId { get; set; }
        public string CoachName { get; set; } = string.Empty;
        public string CoachEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string Status { get; set; } = string.Empty;

        // ── Payout ───────────────────────────────────────────────────────────
        public string? PayOsPayoutId { get; set; }
        public string? PayOsReferenceId { get; set; }
        public string? PayOsPayoutStatus { get; set; }
        public string? FailureReason { get; set; }

        // ── Timestamps ───────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessingAt { get; set; }
        public DateTime? PaidAt { get; set; }

        // ── Bank account (masked) ─────────────────────────────────────────────
        public string BankName { get; set; } = string.Empty;
        public string BankBin { get; set; } = string.Empty;
        public string MaskedAccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;

        // ── Admin ─────────────────────────────────────────────────────────────
        public string? AdminNote { get; set; }

        public string Note { get; set; } =
            "Platform commission was already deducted during booking purchase. " +
            "No additional commission is deducted from this withdrawal.";
    }
}
