using System;
using System.Collections.Generic;

namespace SporticoApp.Application.DTOs.Coaches
{
    public class CoachProfileResponse
    {
        public Guid UserId { get; set; }

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

        public decimal Rating { get; set; }

        public int TotalReviews { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<CoachProfileMediaResponse> Media { get; set; } = new();

        public List<CoachTeachingLocationResponse> TeachingLocations { get; set; } = new();
    }
}
