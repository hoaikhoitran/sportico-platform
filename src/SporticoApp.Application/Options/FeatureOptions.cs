namespace SporticoApp.Application.Options
{
    /// <summary>
    /// Backend feature flags bound from the "Features" configuration section.
    /// </summary>
    public class FeatureOptions
    {
        /// <summary>
        /// When false (production default), the dev/test manual-purchase endpoint
        /// (POST /api/bookings/purchase/manual) is disabled and returns a clean business
        /// error. PayOS remains the normal learner purchase flow. Enable it for dev/test via
        /// Features__EnableManualPurchase=true (already enabled in appsettings.Development.json).
        /// </summary>
        public bool EnableManualPurchase { get; set; } = false;
    }
}
