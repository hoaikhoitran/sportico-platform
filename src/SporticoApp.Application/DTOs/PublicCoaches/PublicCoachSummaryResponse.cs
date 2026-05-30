namespace SporticoApp.Application.DTOs.PublicCoaches
{
    public class PublicCoachListItemResponse
    {
        public Guid CoachId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string? Headline { get; set; }

        public string? Bio { get; set; }

        public int? ExperienceYears { get; set; }

        public string? CoverImageUrl { get; set; }

        public string? TeachingCity { get; set; }

        public string? TeachingDistrict { get; set; }

        public bool? IsOnlineAvailable { get; set; }

        public bool? IsOfflineAvailable { get; set; }

        public string? Specialties { get; set; }

        public decimal Rating { get; set; }

        public int TotalReviews { get; set; }

        public List<PublicCoachSportResponse> Sports { get; set; } = new();
    }

    public class PublicCoachSportResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}