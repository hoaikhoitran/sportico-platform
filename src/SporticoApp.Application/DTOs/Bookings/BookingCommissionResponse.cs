namespace SporticoApp.Application.DTOs.Bookings
{
    public class BookingCommissionResponse
    {
        public decimal TotalAmount { get; set; }

        public decimal PlatformFeeRate { get; set; }

        public decimal PlatformFeeAmount { get; set; }

        public decimal CoachReceiveAmount { get; set; }

        public decimal PerSessionCoachAmount { get; set; }
    }
}
