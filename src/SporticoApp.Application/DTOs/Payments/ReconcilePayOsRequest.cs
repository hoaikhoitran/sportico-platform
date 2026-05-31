namespace SporticoApp.Application.DTOs.Payments
{
    /// <summary>
    /// Body for POST /api/payments/payos/reconcile. Supply either OrderCode or PaymentId.
    /// </summary>
    public class ReconcilePayOsRequest
    {
        public long? OrderCode { get; set; }

        public Guid? PaymentId { get; set; }
    }
}
