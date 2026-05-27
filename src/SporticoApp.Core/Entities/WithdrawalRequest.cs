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

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual CoachWallet Wallet { get; set; } = null!;

    public virtual CoachPayoutAccount? PayoutAccount { get; set; }
}
