using SporticoApp.Application.DTOs.Availability;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICoachAvailabilityService
    {
        Task<Result<CoachAvailabilitySlotResponse>> CreateSlotAsync(
            Guid coachId,
            CreateCoachAvailabilitySlotRequest request);

        Task<Result<PagedResult<CoachAvailabilitySlotResponse>>> GetMySlotsAsync(
            Guid coachId,
            CoachAvailabilitySlotFilterRequest filter);

        Task<Result<PagedResult<CoachAvailabilitySlotResponse>>> GetCoachPublicSlotsAsync(
            Guid coachId,
            CoachAvailabilitySlotFilterRequest filter);

        Task<Result<CoachAvailabilitySlotResponse>> CancelSlotAsync(Guid coachId, Guid slotId);
    }
}
