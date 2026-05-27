namespace SporticoApp.Application.DTOs.TrainingPlans
{
    public class UpdateTrainingPlanRequest
    {
        public string Title { get; set; } = string.Empty;

        public string GoalType { get; set; } = string.Empty;

        public string? Overview { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int TotalWeeks { get; set; }

        public string? Status { get; set; }
    }
}
