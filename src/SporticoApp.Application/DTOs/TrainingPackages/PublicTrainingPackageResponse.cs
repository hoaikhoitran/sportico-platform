using System;

namespace SporticoApp.Application.DTOs.TrainingPackages
{
    /// <summary>
    /// Public training package response that also carries a coach profile summary
    /// so the marketplace frontend can render the coach without extra calls.
    /// </summary>
    public class PublicTrainingPackageResponse
    {
        public Guid Id { get; set; }

        public Guid CoachId { get; set; }

        public int SportId { get; set; }

        public string SportName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int SessionCount { get; set; }

        public int DurationDays { get; set; }

        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        public string? Level { get; set; }

        public string? GoalType { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public PublicCoachSummaryResponse? Coach { get; set; }
    }
}
