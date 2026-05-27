namespace SporticoApp.Application.DTOs.TrainingPlans
{
    public class CreateTrainingPlanDayRequest
    {
        public int DayNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}
