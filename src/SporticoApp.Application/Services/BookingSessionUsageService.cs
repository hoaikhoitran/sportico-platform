using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;

namespace SporticoApp.Application.Services
{
    /// <inheritdoc />
    public class BookingSessionUsageService : IBookingSessionUsageService
    {
        private static readonly IReadOnlyDictionary<string, int> EmptyCounts = new Dictionary<string, int>();

        private readonly ITrainingSessionRepository _trainingSessionRepository;

        public BookingSessionUsageService(ITrainingSessionRepository trainingSessionRepository)
        {
            _trainingSessionRepository = trainingSessionRepository;
        }

        public async Task<BookingSessionUsage> GetAsync(Guid bookingId, int totalSessions)
        {
            var counts = await _trainingSessionRepository.GetStatusCountsByBookingAsync(bookingId);
            return BookingSessionUsage.From(totalSessions, counts);
        }

        public async Task<IReadOnlyDictionary<Guid, BookingSessionUsage>> GetMapAsync(
            IReadOnlyDictionary<Guid, int> bookingTotals)
        {
            if (bookingTotals.Count == 0)
            {
                return new Dictionary<Guid, BookingSessionUsage>();
            }

            var countsByBooking = await _trainingSessionRepository.GetStatusCountsByBookingsAsync(
                bookingTotals.Keys.ToList());

            var result = new Dictionary<Guid, BookingSessionUsage>(bookingTotals.Count);
            foreach (var (bookingId, totalSessions) in bookingTotals)
            {
                var counts = countsByBooking.TryGetValue(bookingId, out var c) ? c : EmptyCounts;
                result[bookingId] = BookingSessionUsage.From(totalSessions, counts);
            }

            return result;
        }
    }
}
