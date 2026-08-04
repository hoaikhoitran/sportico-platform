namespace SporticoApp.Application.DTOs.Bookings
{
    public class PurchaseTrainingPackagePayOsResponse
    {
        public Guid BookingId { get; set; }

        public Guid PaymentId { get; set; }

        /// <summary>Null for a 100%-off voucher booking — PayOS was never called.</summary>
        public long? OrderCode { get; set; }

        /// <summary>Null for a 100%-off voucher booking — no redirect is needed.</summary>
        public string? CheckoutUrl { get; set; } = string.Empty;

        /// <summary>Payment status (pending | paid | failed | cancelled). Kept for backward compatibility.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Same value as <see cref="Status"/>, exposed under an explicit name for new clients.</summary>
        public string PaymentStatus => Status;

        /// <summary>True when the learner must be redirected to PayOS to complete payment.</summary>
        public bool PaymentRequired { get; set; }

        public string BookingStatus { get; set; } = string.Empty;

        public DateTime? ExpiredAt { get; set; }
    }
}
