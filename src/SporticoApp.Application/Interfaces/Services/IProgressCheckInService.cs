using SporticoApp.Application.DTOs.ProgressCheckIns;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IProgressCheckInService
    {
        Task<Result<ProgressCheckInResponse>> CreateAsync(
            Guid learnerId,
            Guid bookingId,
            CreateProgressCheckInRequest request);

        Task<Result<PagedResult<ProgressCheckInResponse>>> GetByBookingAsync(
            Guid userId,
            Guid bookingId,
            ProgressCheckInFilterRequest filter);

        Task<Result<ProgressCheckInResponse>> UpdateFeedbackAsync(
            Guid coachId,
            Guid checkInId,
            UpdateProgressCheckInFeedbackRequest request);
    }
}
