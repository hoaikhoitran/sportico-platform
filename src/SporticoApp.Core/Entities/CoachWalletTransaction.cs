using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Wallet transaction record for coach.
/// </summary>
public partial class CoachWalletTransaction
{
    public Guid Id { get; set; }

    public Guid CoachWalletId { get; set; }

    public Guid CoachId { get; set; }

    /// <summary>
    /// session_release | withdrawal | adjustment
    /// </summary>
    public string Type { get; set; } = null!;

    /// <summary>
    /// credit | debit
    /// </summary>
    public string Direction { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CoachWallet Wallet { get; set; } = null!;
}
