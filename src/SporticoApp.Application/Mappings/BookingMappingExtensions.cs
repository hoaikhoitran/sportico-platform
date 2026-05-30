using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class BookingMappingExtensions
    {
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
                PlatformFeeRate = booking.PlatformFeeRate,
                PlatformFeeAmount = booking.PlatformFeeAmount,
                CoachReceiveAmount = booking.CoachReceiveAmount,
                PerSessionCoachAmount = booking.PerSessionCoachAmount,
                TotalSessions = booking.TotalSessions,
                CompletedSessions = booking.CompletedSessions,
                Status = booking.Status,
                PaidAt = booking.PaidAt,
                CompletedAt = booking.CompletedAt,
                CancelledAt = booking.CancelledAt,
                ExpiresAt = booking.ExpiresAt,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            };
        }
    }
}
