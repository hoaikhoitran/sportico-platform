using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.DTOs.Bookings
{
    /// <summary>
    /// Authoritative, real-time view of how many sessions of a booking's package quota are used.
    /// Computed from actual <c>TrainingSession</c> rows (never the denormalized
    /// <c>Booking.CompletedSessions</c>), so the create-session quota check and every booking
    /// response use exactly the same numbers and can never diverge.
    /// </summary>
    public sealed class BookingSessionUsage
    {
        /// <summary>Total sessions allowed by the package (from the booking).</summary>
        public int TotalSessions { get; init; }

        /// <summary>
        /// Sessions consuming a quota slot (requested + scheduled + completed + missed).
        /// Cancelled sessions are excluded — they release the slot.
        /// </summary>
        public int UsedSessions { get; init; }

        /// <summary>Sessions actually completed.</summary>
        public int CompletedSessions { get; init; }

        /// <summary>Remaining bookable slots = max(0, Total − Used).</summary>
        public int RemainingSessions { get; init; }

        /// <summary>True when the package still has a free slot (RemainingSessions &gt; 0).</summary>
        public bool CanBookSession { get; init; }

        /// <summary>Raw count per session status (only non-zero statuses present).</summary>
        public IReadOnlyDictionary<string, int> SessionCountsByStatus { get; init; }
            = new Dictionary<string, int>();

        /// <summary>
        /// Builds usage from a status→count map. The set of quota-consuming statuses is centralized
        /// in <see cref="TrainingSessionStatuses.BookingQuotaConsuming"/>.
        /// </summary>
        public static BookingSessionUsage From(
            int totalSessions,
            IReadOnlyDictionary<string, int> countsByStatus)
        {
            int CountOf(string status) => countsByStatus.TryGetValue(status, out var c) ? c : 0;

            var used = 0;
            foreach (var status in TrainingSessionStatuses.BookingQuotaConsuming)
            {
                used += CountOf(status);
            }

            var completed = CountOf(TrainingSessionStatuses.Completed);
            var remaining = totalSessions - used;
            if (remaining < 0)
            {
                remaining = 0;
            }

            return new BookingSessionUsage
            {
                TotalSessions = totalSessions,
                UsedSessions = used,
                CompletedSessions = completed,
                RemainingSessions = remaining,
                CanBookSession = remaining > 0,
                SessionCountsByStatus = countsByStatus
            };
        }
    }
}
