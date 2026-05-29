using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Mappings
{
    public static class CoachMappingExtensions
    {

        public static CoachProfile ToEntity(
            this RegisterCoachRequest request,
            Guid userId)
        {
            var now = DateTime.UtcNow;

            return new CoachProfile
            {
                UserId = userId,

                Headline = request.Headline.Trim(),

                Bio = string.IsNullOrWhiteSpace(request.Bio)
                    ? null
                    : request.Bio.Trim(),

                ExperienceYears = request.ExperienceYears,

                Rating = 0,

                TotalReviews = 0,

                CreatedAt = now,

                UpdatedAt = now
            };
        }

        public static void ApplyUpdate(
            this CoachProfile coachProfile,
            UpdateCoachProfileRequest request)
        {
            coachProfile.Headline = Normalize(request.Headline);
            coachProfile.Bio = Normalize(request.Bio);
            coachProfile.ExperienceYears = request.ExperienceYears;
            coachProfile.CoverImageUrl = Normalize(request.CoverImageUrl);
            coachProfile.TeachingAddress = Normalize(request.TeachingAddress);
            coachProfile.TeachingCity = Normalize(request.TeachingCity);
            coachProfile.TeachingDistrict = Normalize(request.TeachingDistrict);
            coachProfile.TeachingLatitude = request.TeachingLatitude;
            coachProfile.TeachingLongitude = request.TeachingLongitude;
            coachProfile.IsOnlineAvailable = request.IsOnlineAvailable;
            coachProfile.IsOfflineAvailable = request.IsOfflineAvailable;
            coachProfile.Specialties = Normalize(request.Specialties);
            coachProfile.CertificationsSummary = Normalize(request.CertificationsSummary);
            coachProfile.AchievementsSummary = Normalize(request.AchievementsSummary);
            coachProfile.FacebookUrl = Normalize(request.FacebookUrl);
            coachProfile.InstagramUrl = Normalize(request.InstagramUrl);
            coachProfile.WebsiteUrl = Normalize(request.WebsiteUrl);
            coachProfile.UpdatedAt = DateTime.UtcNow;
        }

        public static CoachProfileResponse ToResponse(
            this CoachProfile coachProfile)
        {
            return new CoachProfileResponse
            {
                UserId = coachProfile.UserId,

                Headline = coachProfile.Headline,

                Bio = coachProfile.Bio,

                ExperienceYears = coachProfile.ExperienceYears,

                CoverImageUrl = coachProfile.CoverImageUrl,

                TeachingAddress = coachProfile.TeachingAddress,

                TeachingCity = coachProfile.TeachingCity,

                TeachingDistrict = coachProfile.TeachingDistrict,

                TeachingLatitude = coachProfile.TeachingLatitude,

                TeachingLongitude = coachProfile.TeachingLongitude,

                IsOnlineAvailable = coachProfile.IsOnlineAvailable,

                IsOfflineAvailable = coachProfile.IsOfflineAvailable,

                Specialties = coachProfile.Specialties,

                CertificationsSummary = coachProfile.CertificationsSummary,

                AchievementsSummary = coachProfile.AchievementsSummary,

                FacebookUrl = coachProfile.FacebookUrl,

                InstagramUrl = coachProfile.InstagramUrl,

                WebsiteUrl = coachProfile.WebsiteUrl,

                Rating = coachProfile.Rating,

                TotalReviews = coachProfile.TotalReviews,

                CreatedAt = coachProfile.CreatedAt,

                UpdatedAt = coachProfile.UpdatedAt,

                Media = coachProfile.Media
                    .OrderBy(m => m.OrderIndex)
                    .ThenBy(m => m.CreatedAt)
                    .Select(m => m.ToResponse())
                    .ToList(),

                TeachingLocations = coachProfile.TeachingLocations
                    .OrderByDescending(l => l.IsDefault)
                    .ThenBy(l => l.CreatedAt)
                    .Select(l => l.ToResponse())
                    .ToList()
            };
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
