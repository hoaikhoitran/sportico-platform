using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class BookingMappingExtensions
    {
        /// <summary>
        /// Base mapping. Session-usage fields (UsedSessions/RemainingSessions/CanBookSession and the
        /// authoritative CompletedSessions) fall back to the denormalized <c>Booking.CompletedSessions</c>
        /// and are only reliable once enriched with real usage — prefer
        /// <see cref="ToResponse(Booking, BookingSessionUsage)"/> for any learner/coach-facing response.
        /// </summary>
        public static BookingResponse ToResponse(this Booking booking)
        {
            return new BookingResponse
            {
                Id = booking.Id,
                LearnerId = booking.LearnerId,
                CoachId = booking.CoachId,
                TrainingPackageId = booking.TrainingPackageId,
                TrainingPackageTitle = booking.TrainingPackage?.Title ?? string.Empty,
                TotalAmount = booking.TotalAmount,
                OriginalAmount = booking.OriginalAmount,
                DiscountAmount = booking.DiscountAmount,
                VoucherCampaignId = booking.VoucherCampaignId,
                VoucherCode = booking.VoucherCodeSnapshot,
                PlatformFeeRate = booking.PlatformFeeRate,
                PlatformFeeAmount = booking.PlatformFeeAmount,
                CoachReceiveAmount = booking.CoachReceiveAmount,
                PerSessionCoachAmount = booking.PerSessionCoachAmount,
                TotalSessions = booking.TotalSessions,
                CompletedSessions = booking.CompletedSessions,
                UsedSessions = booking.CompletedSessions,
                RemainingSessions = booking.TotalSessions - booking.CompletedSessions,
                CanBookSession = booking.TotalSessions - booking.CompletedSessions > 0,
                Status = booking.Status,
                PaidAt = booking.PaidAt,
                CompletedAt = booking.CompletedAt,
                CancelledAt = booking.CancelledAt,
                ExpiresAt = booking.ExpiresAt,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            };
        }

        /// <summary>
        /// Maps a booking and overwrites the session-usage fields with the authoritative, real-time
        /// usage computed from TrainingSession rows. Use this for all learner/coach booking responses.
        /// </summary>
        public static BookingResponse ToResponse(this Booking booking, BookingSessionUsage usage)
        {
            var response = booking.ToResponse();
            response.TotalSessions = usage.TotalSessions;
            response.CompletedSessions = usage.CompletedSessions;
            response.UsedSessions = usage.UsedSessions;
            response.RemainingSessions = usage.RemainingSessions;
            response.CanBookSession = usage.CanBookSession;
            response.SessionCountsByStatus = usage.SessionCountsByStatus;
            return response;
        }
    }
}
