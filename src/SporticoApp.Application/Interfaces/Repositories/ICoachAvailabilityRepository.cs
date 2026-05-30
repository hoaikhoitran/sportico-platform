using SporticoApp.Application.DTOs.Availability;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICoachAvailabilityRepository
    {
        Task<CoachAvailabilitySlot?> GetByIdAsync(Guid id);

        Task<CoachAvailabilitySlot?> GetByIdForUpdateAsync(Guid id);

        Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetByCoachPagedAsync(
            Guid coachId,
            CoachAvailabilitySlotFilterRequest filter);

        Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetAvailableByCoachPagedAsync(
            Guid coachId,
            CoachAvailabilitySlotFilterRequest filter);

        /// <summary>True if the coach already has a slot whose StartTime..EndTime overlaps with the given range.</summary>
        Task<bool> HasOverlapAsync(Guid coachId, DateTime startTime, DateTime endTime, Guid? excludeSlotId = null);

        Task AddAsync(CoachAvailabilitySlot slot);

        Task SaveChangesAsync();
    }
}
