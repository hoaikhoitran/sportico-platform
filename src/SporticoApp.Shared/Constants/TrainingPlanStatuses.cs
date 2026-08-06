namespace SporticoApp.Shared.Constants
{
    public static class TrainingPlanStatuses
    {
        public const string Draft = "draft";
        public const string Active = "active";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";

        public static readonly string[] All =
        {
            Draft,
            Active,
            Completed,
            Cancelled
        };

        /// <summary>Terminal statuses after which no mutations are allowed.</summary>
        public static readonly string[] Terminal =
        {
            Completed,
            Cancelled
        };
    }
}
