using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Infrastructure.Services.Payments
{
    public class PayOsSettings
    {
        public string ClientId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string ChecksumKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } =
            "https://api-merchant.payos.vn";

        public string ReturnUrl { get; set; } = string.Empty;

        public string CancelUrl { get; set; } = string.Empty;

        public int PaymentLinkExpireMinutes { get; set; } = 15;

        // ── Payout (Chi) settings ────────────────────────────────────────────
        /// <summary>
        /// When true, WithdrawalService.CreateAsync automatically calls PayOS payout API.
        /// When false, the old admin approve → manual-transfer → mark-paid flow is used.
        /// </summary>
        public bool AutoPayoutEnabled { get; set; } = false;

        /// <summary>Default PayOS payout category (e.g. "salary", "business").</summary>
        public string PayoutCategory { get; set; } = "salary";
    }
}
