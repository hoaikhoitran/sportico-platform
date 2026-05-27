namespace SporticoApp.Application.DTOs.Assessments
{
    public class LearnerAssessmentResponse
    {
        public Guid Id { get; set; }

        public Guid BookingId { get; set; }

        public Guid LearnerId { get; set; }

        public Guid CoachId { get; set; }

        public string GoalType { get; set; } = string.Empty;

        public string? GoalDescription { get; set; }

        public decimal? HeightCm { get; set; }

        public decimal? WeightKg { get; set; }

        public decimal? BodyFatPercent { get; set; }

        public string? CurrentLevel { get; set; }

        public string? HealthNotes { get; set; }

        public string? InjuryNotes { get; set; }

        public string? TrainingHistory { get; set; }

        public string? AvailableDaysPerWeek { get; set; }

        public int? PreferredSessionDurationMinutes { get; set; }

        public string? EquipmentAvailable { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
