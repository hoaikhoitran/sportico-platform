namespace SporticoApp.Application.DTOs.Bookings
{
    public class PurchaseTrainingPackagePayOsResponse
    {
        public Guid BookingId { get; set; }

        public Guid PaymentId { get; set; }

        public long OrderCode { get; set; }

        public string CheckoutUrl { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? ExpiredAt { get; set; }
    }
}
