using SporticoApp.Application.DTOs.TrainingSessions;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Mappings
{
    public static class TrainingSessionMappingExtensions
    {
        public static TrainingSession ToEntity(
            this CreateTrainingSessionRequest request,
            Guid learnerId,
            Guid coachId,
            CoachAvailabilitySlot slot)
        {
            var now = DateTime.UtcNow;

            return new TrainingSession
            {
                Id = Guid.NewGuid(),
                BookingId = request.BookingId,
                LearnerId = learnerId,
                CoachId = coachId,
                AvailabilitySlotId = slot.Id,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Location = slot.Location,
                MeetingUrl = slot.MeetingUrl,
                LearnerNote = request.LearnerNote?.Trim(),
                Status = TrainingSessionStatuses.Requested,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        /// <summary>
        /// Builds the training session auto-generated from a package schedule slot when the booking
        /// becomes active. Status is <c>scheduled</c> (no learner request step in the new flow).
        /// </summary>
        public static TrainingSession ToGeneratedSession(
            this TrainingPackageSessionSlot slot,
            Booking booking)
        {
            var now = DateTime.UtcNow;

            return new TrainingSession
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                LearnerId = booking.LearnerId,
                CoachId = booking.CoachId,
                TrainingPackageSessionSlotId = slot.Id,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Location = slot.Location,
                MeetingUrl = slot.MeetingUrl,
                Status = TrainingSessionStatuses.Scheduled,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public static TrainingSessionResponse ToResponse(this TrainingSession session)
        {
            return new TrainingSessionResponse
            {
                Id = session.Id,
                BookingId = session.BookingId,
                LearnerId = session.LearnerId,
                CoachId = session.CoachId,
                TrainingPackageSessionSlotId = session.TrainingPackageSessionSlotId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Status = session.Status,
                MeetingUrl = session.MeetingUrl,
                Location = session.Location,
                LearnerNote = session.LearnerNote,
                CoachNote = session.CoachNote,
                ConfirmedAt = session.ConfirmedAt,
                CompletedAt = session.CompletedAt,
                CancelledAt = session.CancelledAt,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };
        }
    }
}
