namespace SporticoApp.Application.DTOs.TrainingPlans
{
    public class TrainingPlanExerciseResponse
    {
        public Guid Id { get; set; }

        public Guid TrainingPlanDayId { get; set; }

        public string ExerciseName { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        public int? Sets { get; set; }

        public string? Reps { get; set; }

        public string? Intensity { get; set; }

        public int? RestSeconds { get; set; }

        public string? Notes { get; set; }
    }
}
