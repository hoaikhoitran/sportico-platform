using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Log raw response từ Payment gateway (audit trail)
/// </summary>
public partial class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid payment_id { get; set; }

    public string? GatewayResponse { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Payment Payment { get; set; } = null!;
}
