using SporticoApp.Application.DTOs.Availability;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class CoachAvailabilityMappingExtensions
    {
        public static CoachAvailabilitySlotResponse ToResponse(this CoachAvailabilitySlot slot)
        {
            return new CoachAvailabilitySlotResponse
            {
                Id = slot.Id,
                CoachId = slot.CoachId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Status = slot.Status,
                Location = slot.Location,
                MeetingUrl = slot.MeetingUrl,
                IsOnline = slot.IsOnline,
                Note = slot.Note,
                CreatedAt = slot.CreatedAt,
                UpdatedAt = slot.UpdatedAt
            };
        }
    }
}
