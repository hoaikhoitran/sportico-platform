using SporticoApp.Application.DTOs.Availability;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class CoachAvailabilityMappingExtensions
    {
        /// <summary>
        /// Maps a slot to its response. <paramref name="bookedParticipants"/> is the number of active
        /// sessions on the slot — callers MUST pass the real count (it is never assumed to be 0 for a
        /// persisted slot that may already have bookings).
        /// </summary>
        public static CoachAvailabilitySlotResponse ToResponse(
            this CoachAvailabilitySlot slot,
            int bookedParticipants)
        {
            var remaining = slot.MaxParticipants - bookedParticipants;
            if (remaining < 0)
            {
                remaining = 0;
            }

            return new CoachAvailabilitySlotResponse
            {
                Id = slot.Id,
                CoachId = slot.CoachId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Status = slot.Status,
                MaxParticipants = slot.MaxParticipants,
                BookedParticipants = bookedParticipants,
                RemainingParticipants = remaining,
                IsFull = remaining <= 0,
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
