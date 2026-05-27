using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IBookingService
    {
        Task<Result<BookingResponse>> PurchaseManualAsync(
            Guid learnerId,
            PurchaseTrainingPackageManualRequest request);

        Task<Result<PurchaseTrainingPackagePayOsResponse>> PurchaseWithPayOsAsync(
            Guid learnerId,
            PurchaseTrainingPackagePayOsRequest request);

        Task<Result<object>> HandlePayOsWebhookAsync(
            PayOsWebhookRequest request);

        Task<Result<PagedResult<BookingResponse>>> GetMyBookingsAsync(
            Guid learnerId,
            BookingFilterRequest filter);

        Task<Result<BookingResponse>> GetMyBookingByIdAsync(
            Guid learnerId,
            Guid bookingId);

        Task<Result<PagedResult<BookingResponse>>> GetCoachBookingsAsync(
            Guid coachId,
            BookingFilterRequest filter);

        Task<Result<BookingResponse>> GetCoachBookingByIdAsync(
            Guid coachId,
            Guid bookingId);
    }
}
