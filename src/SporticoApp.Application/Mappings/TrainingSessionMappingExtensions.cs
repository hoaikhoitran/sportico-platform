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
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new TrainingSession
            {
                Id = Guid.NewGuid(),
                BookingId = request.BookingId,
                LearnerId = learnerId,
                CoachId = coachId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Location = request.Location?.Trim(),
                MeetingUrl = request.MeetingUrl?.Trim(),
                LearnerNote = request.LearnerNote?.Trim(),
                Status = TrainingSessionStatuses.Requested,
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
