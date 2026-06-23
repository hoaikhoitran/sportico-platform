namespace SporticoApp.Application.DTOs.TrainingPackages
{
    public class TrainingPackageResponse
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

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        public string? Level { get; set; }

        public string? GoalType { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? RejectionReason { get; set; }

        public Guid? ReviewedByUserId { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        /// <summary>The fixed schedule of sessions defined for this package.</summary>
        public List<TrainingPackageSessionResponse> Sessions { get; set; } = new();
    }
}
