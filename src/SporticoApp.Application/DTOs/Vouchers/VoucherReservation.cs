namespace SporticoApp.Application.DTOs.Vouchers
{
    /// <summary>
    /// Internal result of reserving a voucher for a new booking (BookingService → VoucherService).
    /// Not exposed directly over HTTP.
    /// </summary>
    public class VoucherReservation
    {
        public Guid RedemptionId { get; set; }

        public Guid VoucherCampaignId { get; set; }

        public decimal DiscountAmount { get; set; }

        public string CodeSnapshot { get; set; } = string.Empty;

        public string DiscountTypeSnapshot { get; set; } = string.Empty;

        public decimal DiscountValueSnapshot { get; set; }

        public decimal? MaxDiscountAmountSnapshot { get; set; }
    }
}
