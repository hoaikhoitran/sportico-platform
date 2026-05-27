namespace SporticoApp.Application.DTOs.ProgressCheckIns
{
    public class ProgressCheckInResponse
    {
        public Guid Id { get; set; }

        public Guid BookingId { get; set; }

        public Guid LearnerId { get; set; }

        public Guid CoachId { get; set; }

        public DateTime CheckInDate { get; set; }

        public decimal? WeightKg { get; set; }

        public decimal? BodyFatPercent { get; set; }

        public decimal? WaistCm { get; set; }

        public string? EnergyLevel { get; set; }

        public string? SleepQuality { get; set; }

        public string? LearnerNote { get; set; }

        public string? CoachFeedback { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
