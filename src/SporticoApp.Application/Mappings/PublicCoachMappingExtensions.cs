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
                FullName = coach.User?.FullName ?? string.Empty,
                AvatarUrl = coach.User?.AvatarUrl,
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
                Sports = MapSports(coach.CoachSports)
            };
        }

        public static PublicCoachDetailResponse ToPublicDetailResponse(
            this CoachProfile coach)
        {
            var response = new PublicCoachDetailResponse
            {
                CoachId = coach.UserId,
                FullName = coach.User?.FullName ?? string.Empty,
                AvatarUrl = coach.User?.AvatarUrl,
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
                Sports = MapSports(coach.CoachSports),
                Media = (coach.Media ?? Enumerable.Empty<CoachProfileMedia>())
                    .Where(x => x.MediaType != "identity")
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
                TrainingPackages = (coach.TrainingPackages ?? Enumerable.Empty<TrainingPackage>())
                    .Where(x => x.Status == TrainingPackageStatuses.Published)
                    .Select(x => new PublicCoachTrainingPackageResponse
                    {
                        Id = x.Id,
                        SportId = x.SportId,
                        SportName = x.Sport != null ? x.Sport.Name : string.Empty,
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

        /// <summary>
        /// Maps coach-sport links to public sport DTOs. Skips links whose Sport
        /// navigation is missing (e.g. an orphaned coach_sports row pointing at a
        /// deleted sport) or inactive, so a bad relation can never throw.
        /// </summary>
        private static List<PublicCoachSportResponse> MapSports(
            ICollection<CoachSport>? coachSports)
        {
            return (coachSports ?? Enumerable.Empty<CoachSport>())
                .Where(x => x.Sport != null && x.Sport.IsActive)
                .Select(x => new PublicCoachSportResponse
                {
                    Id = x.SportId,
                    Name = x.Sport.Name
                })
                .ToList();
        }
    }
}
