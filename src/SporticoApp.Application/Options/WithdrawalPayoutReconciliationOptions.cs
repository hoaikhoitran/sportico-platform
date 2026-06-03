namespace SporticoApp.Application.Options
{
    /// <summary>
    /// Options for the background job that reconciles PayOS payout status for withdrawals that are
    /// still <c>processing</c> (PayOS accepted the payout but had not yet reported a terminal state
    /// at create time). Bound from the "WithdrawalPayoutReconciliation" configuration section.
    /// </summary>
    public class WithdrawalPayoutReconciliationOptions
    {
        public const string SectionName = "WithdrawalPayoutReconciliation";

        /// <summary>
        /// When true, the background reconciliation loop runs. Default true so payouts are
        /// finalized automatically without extra configuration. Set
        /// <c>WithdrawalPayoutReconciliation__Enabled=false</c> to disable.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Seconds between reconciliation passes. Clamped to a sane minimum at runtime.</summary>
        public int IntervalSeconds { get; set; } = 60;

        /// <summary>Maximum number of processing withdrawals reconciled per pass.</summary>
        public int BatchSize { get; set; } = 20;
    }
}
