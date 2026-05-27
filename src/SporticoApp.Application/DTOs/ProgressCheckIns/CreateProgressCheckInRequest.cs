namespace SporticoApp.Application.DTOs.ProgressCheckIns
{
    public class CreateProgressCheckInRequest
    {
        public DateTime CheckInDate { get; set; }

        public decimal? WeightKg { get; set; }

        public decimal? BodyFatPercent { get; set; }

        public decimal? WaistCm { get; set; }

        public string? EnergyLevel { get; set; }

        public string? SleepQuality { get; set; }

        public string? LearnerNote { get; set; }
    }
}
