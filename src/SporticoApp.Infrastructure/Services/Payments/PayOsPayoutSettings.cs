namespace SporticoApp.Infrastructure.Services.Payments
{
    /// <summary>
    /// Credentials for the PayOS Payout / "Kênh chuyển tiền" (Chi) channel — used ONLY for
    /// outbound coach withdrawals. These are deliberately separate from <see cref="PayOsSettings"/>
    /// (the inbound payment-link / "Kênh thanh toán" channel) because PayOS may issue different
    /// ClientId / ApiKey / ChecksumKey per channel.
    ///
    /// Bound from the "PayOsPayout" configuration section. Real values must come from Azure App
    /// Settings / environment variables (PayOsPayout__ClientId, etc.) — never committed.
    /// </summary>
    public class PayOsPayoutSettings
    {
        public string ClientId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string ChecksumKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";

        /// <summary>
        /// When true, admin approval of a withdrawal triggers an automatic PayOS payout.
        /// When false, the manual approve → external-transfer → mark-paid flow is used.
        /// </summary>
        public bool AutoPayoutEnabled { get; set; } = false;

        /// <summary>
        /// Optional PayOS payout category (e.g. "salary", "business").
        /// Leave null/empty to omit category from the request body entirely.
        /// </summary>
        public string? PayoutCategory { get; set; }
    }
}
