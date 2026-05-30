namespace SporticoApp.Application.DTOs.Payments
{
    public class PayOsPayoutBalanceResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public decimal AvailableBalance { get; set; }
        public string Currency { get; set; } = "VND";
        public string? RawJson { get; set; }
    }
}
