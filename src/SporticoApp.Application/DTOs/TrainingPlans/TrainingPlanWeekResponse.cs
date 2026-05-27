using System.Collections.Generic;

namespace SporticoApp.Application.DTOs.TrainingPlans
{
    public class TrainingPlanWeekResponse
    {
        public Guid Id { get; set; }

        public Guid TrainingPlanId { get; set; }

        public int WeekNumber { get; set; }

        public string? Focus { get; set; }

        public string? Notes { get; set; }

        public List<TrainingPlanDayResponse> Days { get; set; } = new();
    }
}
