namespace SporticoApp.Shared.Constants
{
    public static class TrainingPackageSessionSlotStatuses
    {
        /// <summary>At least one seat is still available.</summary>
        public const string Open = "open";

        /// <summary>All seats are reserved/consumed (BookedParticipants == MaxParticipants).</summary>
        public const string Full = "full";

        /// <summary>Slot is no longer offered.</summary>
        public const string Cancelled = "cancelled";
    }
}
