namespace SporticoApp.Application.DTOs.Payments
{
    /// <summary>
    /// Parsed result of a PayOS "get payment-request information" call
    /// (GET /v2/payment-requests/{id}). Used by the reconcile flow to verify the
    /// real payment state against PayOS instead of trusting frontend query strings.
    /// </summary>
    public class PayOsPaymentStatusResult
    {
        /// <summary>PayOS envelope code ("00" = success envelope; not the payment state).</summary>
        public string Code { get; set; } = string.Empty;

        public string Desc { get; set; } = string.Empty;

        public long OrderCode { get; set; }

        /// <summary>
        /// PayOS payment state, upper-cased: PENDING | PROCESSING | PAID | CANCELLED | EXPIRED.
        /// Empty when PayOS did not return a data object.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        public int Amount { get; set; }

        public int AmountPaid { get; set; }

        /// <summary>Raw JSON kept for reconciliation/audit. Never contains API keys.</summary>
        public string RawJson { get; set; } = string.Empty;
    }
}
