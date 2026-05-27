namespace SporticoApp.Application.DTOs.TrainingPlans
{
    public class CreateTrainingPlanWeekRequest
    {
        public int WeekNumber { get; set; }

        public string? Focus { get; set; }

        public string? Notes { get; set; }
    }
}
