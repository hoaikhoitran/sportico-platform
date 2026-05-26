using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Giao dịch thanh toán
/// </summary>
public partial class Payment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = null!;

    /// <summary>
    /// Polymorphic: liên kết với coach_package hoặc đối tượng khác
    /// </summary>
    public string ReferenceType { get; set; } = null!;

    /// <summary>
    /// ID của đối tượng được thanh toán (vd: CoachPackage.Id)
    /// </summary>
    public Guid? ReferenceId { get; set; }

    public string Status { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }
    public long? OrderCode { get; set; }

    public string? PaymentLinkId { get; set; }

    public string? CheckoutUrl { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual User User { get; set; } = null!;
}
