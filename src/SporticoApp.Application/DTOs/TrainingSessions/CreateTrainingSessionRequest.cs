namespace SporticoApp.Application.DTOs.TrainingSessions
{
    public class CreateTrainingSessionRequest
    {
        public Guid BookingId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string? Location { get; set; }

        public string? MeetingUrl { get; set; }

        public string? LearnerNote { get; set; }
    }
}
