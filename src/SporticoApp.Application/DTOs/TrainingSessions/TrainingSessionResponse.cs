namespace SporticoApp.Application.DTOs.TrainingSessions
{
    public class TrainingSessionResponse
    {
        public Guid Id { get; set; }

        public Guid BookingId { get; set; }

        public Guid LearnerId { get; set; }

        public Guid CoachId { get; set; }

        /// <summary>Set when the session was auto-generated from a package schedule slot; null for legacy sessions.</summary>
        public Guid? TrainingPackageSessionSlotId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? MeetingUrl { get; set; }

        public string? Location { get; set; }

        public string? LearnerNote { get; set; }

        public string? CoachNote { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
