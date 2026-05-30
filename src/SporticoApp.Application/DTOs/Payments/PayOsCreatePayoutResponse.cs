namespace SporticoApp.Application.DTOs.Payments
{
    public class PayOsCreatePayoutResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public PayOsPayoutData? Data { get; set; }
        public string? RawJson { get; set; }
    }
}
