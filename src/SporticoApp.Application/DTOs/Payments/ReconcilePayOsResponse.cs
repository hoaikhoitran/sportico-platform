namespace SporticoApp.Application.DTOs.Payments
{
    /// <summary>
    /// Result of a PayOS reconcile call. Reflects the authoritative backend state
    /// after (optionally) verifying against PayOS — never a guess from the frontend.
    /// </summary>
    public class ReconcilePayOsResponse
    {
        public Guid PaymentId { get; set; }

        public long? OrderCode { get; set; }

        /// <summary>Backend payment status: pending | paid | cancelled | failed.</summary>
        public string PaymentStatus { get; set; } = string.Empty;

        public Guid? BookingId { get; set; }

        /// <summary>Backend booking status: pending_payment | active | cancelled | ...</summary>
        public string? BookingStatus { get; set; }

        /// <summary>True when the booking is active (whether activated now or already active).</summary>
        public bool Activated { get; set; }

        /// <summary>Last status PayOS reported during this reconcile, if it was queried.</summary>
        public string? PayOsStatus { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
