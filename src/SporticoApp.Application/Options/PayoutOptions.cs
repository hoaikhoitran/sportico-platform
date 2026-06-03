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

        /// <summary>
        /// PayOS payout category sent with Chi payouts.
        /// Null/empty means category is omitted from the request body entirely.
        /// Only set this when the PayOS merchant account requires a specific category.
        /// Do not default to "salary" — PayOS rejects unrecognised category values.
        /// Configure via PayOsPayout__PayoutCategory (env) when needed.
        /// </summary>
        public string? PayoutCategory { get; set; }
    }
}
