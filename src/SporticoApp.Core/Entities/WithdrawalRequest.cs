using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Withdrawal request from coach wallet.
/// </summary>
public partial class WithdrawalRequest
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    public Guid CoachWalletId { get; set; }

    public Guid? CoachPayoutAccountId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>
    /// pending | approved | rejected | paid | cancelled
    /// </summary>
    public string Status { get; set; } = null!;

    public string? AdminNote { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Set when the PayOS payout is initiated successfully.</summary>
    public DateTime? ProcessingAt { get; set; }

    /// <summary>Set when the payout is confirmed as paid.</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>PayOS payout identifier returned when a payout is created.</summary>
    public string? PayOsPayoutId { get; set; }

    /// <summary>Internal reference id sent to PayOS for idempotency (typically WithdrawalRequest.Id).</summary>
    public string? PayOsReferenceId { get; set; }

    /// <summary>Last known payout status from PayOS (e.g. processing, success, failed).</summary>
    public string? PayOsPayoutStatus { get; set; }

    /// <summary>Raw JSON response from the PayOS payout API, kept for reconciliation.</summary>
    public string? PayOsRawResponse { get; set; }

    /// <summary>Human-readable reason when a payout fails.</summary>
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual CoachWallet Wallet { get; set; } = null!;

    public virtual CoachPayoutAccount? PayoutAccount { get; set; }
}
