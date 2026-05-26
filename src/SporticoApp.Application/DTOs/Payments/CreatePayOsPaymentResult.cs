namespace SporticoApp.Application.DTOs.Payments
{
    public class CreatePayOsPaymentResult
    {
        public string PaymentLinkId { get; set; } = string.Empty;

        public string CheckoutUrl { get; set; } = string.Empty;

        public DateTime? ExpiredAt { get; set; }
    }
}