using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Internal wallet for coach.
/// </summary>
public partial class CoachWallet
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    public decimal AvailableBalance { get; set; }

    public decimal PendingBalance { get; set; }

    public decimal TotalEarned { get; set; }

    public decimal TotalWithdrawn { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Application-managed optimistic concurrency token.
    /// Incremented on every wallet mutation to prevent concurrent double-spend.
    /// </summary>
    public int Version { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual ICollection<CoachWalletTransaction> Transactions { get; set; } = new List<CoachWalletTransaction>();
}
