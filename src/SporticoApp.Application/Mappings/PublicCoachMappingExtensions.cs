using SporticoApp.Application.DTOs.PublicCoaches;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Mappings
{
    public static class PublicCoachMappingExtensions
    {
        public static PublicCoachListItemResponse ToPublicSummaryResponse(
            this CoachProfile coach)
        {
            return new PublicCoachListItemResponse
            {
                CoachId = coach.UserId,
                FullName = coach.User.FullName,
                AvatarUrl = coach.User.AvatarUrl,
                Headline = coach.Headline,
                Bio = coach.Bio,
                ExperienceYears = coach.ExperienceYears,
                CoverImageUrl = coach.CoverImageUrl,
                TeachingCity = coach.TeachingCity,
                TeachingDistrict = coach.TeachingDistrict,
                IsOnlineAvailable = coach.IsOnlineAvailable,
                IsOfflineAvailable = coach.IsOfflineAvailable,
                Specialties = coach.Specialties,
                Rating = coach.Rating,
                TotalReviews = coach.TotalReviews,
                Sports = coach.CoachSports
                    .Where(x => x.Sport.IsActive)
                    .Select(x => new PublicCoachSportResponse
                    {
                        Id = x.SportId,
                        Name = x.Sport.Name
                    })
                    .ToList()
            };
        }

        public static PublicCoachDetailResponse ToPublicDetailResponse(
            this CoachProfile coach)
        {
            var response = new PublicCoachDetailResponse
            {
                CoachId = coach.UserId,
                FullName = coach.User.FullName,
                AvatarUrl = coach.User.AvatarUrl,
                Headline = coach.Headline,
                Bio = coach.Bio,
                ExperienceYears = coach.ExperienceYears,
                CoverImageUrl = coach.CoverImageUrl,
                TeachingAddress = coach.TeachingAddress,
                TeachingCity = coach.TeachingCity,
                TeachingDistrict = coach.TeachingDistrict,
                IsOnlineAvailable = coach.IsOnlineAvailable,
                IsOfflineAvailable = coach.IsOfflineAvailable,
                Specialties = coach.Specialties,
                CertificationsSummary = coach.CertificationsSummary,
                AchievementsSummary = coach.AchievementsSummary,
                Rating = coach.Rating,
                TotalReviews = coach.TotalReviews,
                Sports = coach.CoachSports
                    .Where(x => x.Sport.IsActive)
                    .Select(x => new PublicCoachSportResponse
                    {
                        Id = x.SportId,
                        Name = x.Sport.Name
                    })
                    .ToList(),
                Media = coach.Media
                    .Where(x =>
                        x.MediaType != "identity")
                    .OrderBy(x => x.OrderIndex)
                    .Select(x => new PublicCoachMediaResponse
                    {
                        Id = x.Id,
                        MediaType = x.MediaType,
                        MediaUrl = x.MediaUrl,
                        Title = x.Title,
                        Description = x.Description,
                        OrderIndex = x.OrderIndex
                    })
                    .ToList(),
                TrainingPackages = coach.TrainingPackages
                    .Where(x => x.Status == TrainingPackageStatuses.Published)
                    .Select(x => new PublicCoachTrainingPackageResponse
                    {
                        Id = x.Id,
                        SportId = x.SportId,
                        SportName = x.Sport.Name,
                        Title = x.Title,
                        Description = x.Description,
                        Price = x.Price,
                        SessionCount = x.SessionCount,
                        DurationDays = x.DurationDays,
                        Location = x.Location,
                        IsOnline = x.IsOnline,
                        Level = x.Level,
                        GoalType = x.GoalType,
                        Status = x.Status
                    })
                    .ToList()
            };

            return response;
        }
    }
}