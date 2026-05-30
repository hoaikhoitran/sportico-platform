namespace SporticoApp.Application.DTOs.PublicCoaches
{
    public class PublicCoachDetailResponse : PublicCoachListItemResponse
    {
        public string? TeachingAddress { get; set; }

        public string? CertificationsSummary { get; set; }

        public string? AchievementsSummary { get; set; }

        public List<PublicCoachMediaResponse> Media { get; set; } = new();

        public List<PublicCoachTrainingPackageResponse> TrainingPackages { get; set; } = new();
    }

    public class PublicCoachMediaResponse
    {
        public Guid Id { get; set; }

        public string MediaType { get; set; } = string.Empty;

        public string MediaUrl { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string? Description { get; set; }

        public int OrderIndex { get; set; }
    }

    public class PublicCoachTrainingPackageResponse
    {
        public Guid Id { get; set; }

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

        public string Status { get; set; } = "published";
    }
}