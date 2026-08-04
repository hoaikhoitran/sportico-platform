namespace SporticoApp.Application.DTOs.Vouchers
{
    /// <summary>Server-computed preview. Never trust an equivalent payload sent by the client at purchase time.</summary>
    public class VoucherQuoteResponse
    {
        public string Code { get; set; } = string.Empty;

        public decimal OriginalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public string DiscountType { get; set; } = string.Empty;

        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }
    }
}
