namespace SporticoApp.Application.DTOs.Coaches
{
    public class UpdateCoachProfileRequest
    {
        public string? Headline { get; set; }

        public string? Bio { get; set; }

        public int? ExperienceYears { get; set; }

        public string? CoverImageUrl { get; set; }

        public string? TeachingAddress { get; set; }

        public string? TeachingCity { get; set; }

        public string? TeachingDistrict { get; set; }

        public decimal? TeachingLatitude { get; set; }

        public decimal? TeachingLongitude { get; set; }

        public bool? IsOnlineAvailable { get; set; }

        public bool? IsOfflineAvailable { get; set; }

        public string? Specialties { get; set; }

        public string? CertificationsSummary { get; set; }

        public string? AchievementsSummary { get; set; }

        public string? FacebookUrl { get; set; }

        public string? InstagramUrl { get; set; }

        public string? WebsiteUrl { get; set; }
    }
}
