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

        /// <summary>
        /// Statuses that consume a session slot of a booking's package quota (`usedSessions`).
        /// A learner can book until <c>usedSessions &gt;= Booking.TotalSessions</c>.
        ///
        /// Rule (authoritative — used by BOTH the create-session quota check and the booking
        /// response so the two never diverge):
        ///   - requested, scheduled, completed, missed  → consume a slot
        ///   - cancelled                                → releases the slot (does NOT consume)
        ///
        /// Rationale: a learner who has requested/scheduled all sessions has used the package even
        /// before completion; a no-show (missed) still consumes the session; only an explicit
        /// cancellation frees the slot for re-booking.
        /// </summary>
        public static readonly string[] BookingQuotaConsuming = { Requested, Scheduled, Completed, Missed };
    }
}
