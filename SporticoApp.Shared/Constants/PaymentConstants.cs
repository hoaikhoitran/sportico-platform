namespace SporticoApp.Shared.Constants
{
    public static class PaymentMethods
    {
        public const string PayOs = "payos";
        public const string Manual = "manual";
    }

    public static class PaymentStatuses
    {
        public const string Pending = "pending";
        public const string Paid = "paid";
        public const string Failed = "failed";
        public const string Cancelled = "cancelled";
    }

    public static class PaymentReferenceTypes
    {
        public const string CoachPackage = "coach_package";
        public const string Booking = "booking";
    }

    public static class CoachPackageStatuses
    {
        public const string Pending = "pending";
        public const string Active = "active";
        public const string Expired = "expired";
        public const string Cancelled = "cancelled";
    }
}