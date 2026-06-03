namespace SporticoApp.Shared.Constants
{
    public static class TrainingSessionStatuses
    {
        public const string Requested = "requested";
        public const string Scheduled = "scheduled";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";
        public const string Missed = "missed";

        /// <summary>
        /// Statuses that occupy a seat on an availability slot for capacity purposes.
        /// Cancelled / missed sessions release the seat. Matches the statuses the former
        /// filtered unique index used.
        /// </summary>
        public static readonly string[] CapacityOccupying = { Requested, Scheduled, Completed };
    }
}
