using System.Collections.Generic;

namespace SporticoApp.Application.DTOs.TrainingPlans
{
    public class TrainingPlanResponse
    {
        public Guid Id { get; set; }

        public Guid BookingId { get; set; }

        public Guid LearnerId { get; set; }

        public Guid CoachId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string GoalType { get; set; } = string.Empty;

        public string? Overview { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int TotalWeeks { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<TrainingPlanWeekResponse> Weeks { get; set; } = new();
    }
}
