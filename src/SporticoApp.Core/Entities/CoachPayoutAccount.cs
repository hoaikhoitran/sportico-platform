using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Coach payout account details.
/// </summary>
public partial class CoachPayoutAccount
{
    public Guid Id { get; set; }

    public Guid CoachId { get; set; }

    public string PayoutMethod { get; set; } = null!;

    public string BankName { get; set; } = null!;

    /// <summary>6-digit bank BIN required by the PayOS payout toBin field.</summary>
    public string BankBin { get; set; } = null!;

    public string BankAccountNumber { get; set; } = null!;

    public string BankAccountHolder { get; set; } = null!;

    /// <summary>
    /// pending | verified | rejected
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;
}
