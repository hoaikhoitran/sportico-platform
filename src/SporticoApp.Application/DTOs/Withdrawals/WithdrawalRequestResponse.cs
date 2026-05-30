namespace SporticoApp.Application.DTOs.Withdrawals
{
    public class WithdrawalRequestResponse
    {
        public Guid Id { get; set; }
        public Guid CoachId { get; set; }
        public Guid CoachWalletId { get; set; }
        public Guid? CoachPayoutAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AdminNote { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        // ── Payout fields ────────────────────────────────────────────────────
        public string? PayOsPayoutId { get; set; }
        public string? PayOsReferenceId { get; set; }
        public string? PayOsPayoutStatus { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? ProcessingAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
