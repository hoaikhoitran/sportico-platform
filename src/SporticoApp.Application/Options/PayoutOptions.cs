namespace SporticoApp.Application.Options
{
    /// <summary>
    /// Application-layer options for the coach payout flow.
    /// Bound from the PayOs configuration section via Infrastructure DI.
    /// </summary>
    public class PayoutOptions
    {
        /// <summary>
        /// When true, WithdrawalService.CreateAsync calls PayOS payout automatically.
        /// When false, admin manually approves and marks paid.
        /// </summary>
        public bool AutoPayoutEnabled { get; set; } = false;

        /// <summary>PayOS payout category (e.g. "salary").</summary>
        public string PayoutCategory { get; set; } = "salary";
    }
}
