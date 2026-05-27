using System.Collections.Generic;

namespace SporticoApp.Application.DTOs.TrainingPlans
{
    public class TrainingPlanDayResponse
    {
        public Guid Id { get; set; }

        public Guid TrainingPlanWeekId { get; set; }

        public int DayNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public List<TrainingPlanExerciseResponse> Exercises { get; set; } = new();
    }
}
