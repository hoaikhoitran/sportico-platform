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

        /// <summary>Expiry of the underlying purchased package (Booking.ExpiresAt). Null if not set.</summary>
        public DateTime? BookingExpiresAt { get; set; }

        /// <summary>True when the plan can no longer be edited (terminal status or expired package).</summary>
        public bool IsReadOnly { get; set; }

        /// <summary>Human-readable reason the plan is read-only, or null when editable.</summary>
        public string? ReadOnlyReason { get; set; }

        public List<TrainingPlanWeekResponse> Weeks { get; set; } = new();
    }
}
