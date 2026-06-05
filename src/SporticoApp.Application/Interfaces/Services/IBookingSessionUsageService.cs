using SporticoApp.Application.DTOs.Bookings;

namespace SporticoApp.Application.Interfaces.Services
{
    /// <summary>
    /// Single source of truth for a booking's session-quota usage, computed from real
    /// <c>TrainingSession</c> rows scoped strictly by <c>bookingId</c>. Used by both the
    /// create-session quota check and every booking response so they never diverge.
    /// </summary>
    public interface IBookingSessionUsageService
    {
        /// <summary>Usage for one booking.</summary>
        Task<BookingSessionUsage> GetAsync(Guid bookingId, int totalSessions);

        /// <summary>
        /// Usage for many bookings in a single grouped query (no N+1).
        /// <paramref name="bookingTotals"/> maps each booking id to its <c>TotalSessions</c>.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, BookingSessionUsage>> GetMapAsync(
            IReadOnlyDictionary<Guid, int> bookingTotals);
    }
}
