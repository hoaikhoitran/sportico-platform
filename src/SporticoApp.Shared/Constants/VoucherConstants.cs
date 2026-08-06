namespace SporticoApp.Shared.Constants
{
    public static class VoucherCampaignStatuses
    {
        public const string Draft = "draft";
        public const string Active = "active";
        public const string Paused = "paused";
        public const string Ended = "ended";

        public static readonly string[] All = { Draft, Active, Paused, Ended };
    }

    public static class VoucherDiscountTypes
    {
        public const string FixedAmount = "fixed_amount";
        public const string Percentage = "percentage";

        public static readonly string[] All = { FixedAmount, Percentage };
    }

    /// <summary>
    /// reserved: seat held while a PayOS payment is pending.
    /// applied: payment confirmed paid — the redemption is permanently counted as used.
    /// released: payment cancelled/failed/expired — the reserved usage/budget is given back.
    /// Terminal states (applied/released) never transition again.
    /// </summary>
    public static class VoucherRedemptionStatuses
    {
        public const string Reserved = "reserved";
        public const string Applied = "applied";
        public const string Released = "released";
    }
}
