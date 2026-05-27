namespace SporticoApp.Application.DTOs.TrainingPlans
{
    public class UpdateTrainingPlanExerciseRequest
    {
        public string ExerciseName { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        public int? Sets { get; set; }

        public string? Reps { get; set; }

        public string? Intensity { get; set; }

        public int? RestSeconds { get; set; }

        public string? Notes { get; set; }
    }
}
