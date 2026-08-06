namespace SporticoApp.Shared.Constants
{
    public static class WithdrawalRequestStatuses
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        /// <summary>PayOS payout has been accepted but final confirmation is pending.</summary>
        public const string Processing = "processing";
        public const string Paid = "paid";
        public const string Rejected = "rejected";
        /// <summary>PayOS payout failed; reserved funds returned to AvailableBalance.</summary>
        public const string Failed = "failed";
        public const string Cancelled = "cancelled";
    }
}
