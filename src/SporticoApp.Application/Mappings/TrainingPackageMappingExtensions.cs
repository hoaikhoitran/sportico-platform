using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Mappings
{
    public static class TrainingPackageMappingExtensions
    {
        public static TrainingPackage ToEntity(
            this CreateTrainingPackageRequest request,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new TrainingPackage
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                SportId = request.SportId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Price = request.Price,
                SessionCount = request.SessionCount,
                DurationDays = request.DurationDays,
                Location = request.Location?.Trim(),
                IsOnline = request.IsOnline,
                Level = request.Level?.Trim(),
                GoalType = request.GoalType?.Trim(),
                Status = TrainingPackageStatuses.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public static void ApplyUpdate(
            this TrainingPackage trainingPackage,
            UpdateTrainingPackageRequest request)
        {
            trainingPackage.SportId = request.SportId;
            trainingPackage.Title = request.Title.Trim();
            trainingPackage.Description = request.Description?.Trim();
            trainingPackage.Price = request.Price;
            trainingPackage.SessionCount = request.SessionCount;
            trainingPackage.DurationDays = request.DurationDays;
            trainingPackage.Location = request.Location?.Trim();
            trainingPackage.IsOnline = request.IsOnline;
            trainingPackage.Level = request.Level?.Trim();
            trainingPackage.GoalType = request.GoalType?.Trim();
            trainingPackage.Status = TrainingPackageStatuses.Pending;
            trainingPackage.RejectionReason = null;
            trainingPackage.ReviewedAt = null;
            trainingPackage.ReviewedByUserId = null;
            trainingPackage.UpdatedAt = DateTime.UtcNow;
        }

        public static PublicTrainingPackageResponse ToPublicResponse(
            this TrainingPackage trainingPackage)
        {
            return new PublicTrainingPackageResponse
            {
                Id = trainingPackage.Id,
                CoachId = trainingPackage.CoachId,
                SportId = trainingPackage.SportId,
                SportName = trainingPackage.Sport?.Name ?? string.Empty,
                Title = trainingPackage.Title,
                Description = trainingPackage.Description,
                Price = trainingPackage.Price,
                SessionCount = trainingPackage.SessionCount,
                DurationDays = trainingPackage.DurationDays,
                Location = trainingPackage.Location,
                IsOnline = trainingPackage.IsOnline,
                Level = trainingPackage.Level,
                GoalType = trainingPackage.GoalType,
                Status = trainingPackage.Status,
                CreatedAt = trainingPackage.CreatedAt,
                UpdatedAt = trainingPackage.UpdatedAt,
                Coach = trainingPackage.Coach == null
                    ? null
                    : new PublicCoachSummaryResponse
                    {
                        CoachId = trainingPackage.CoachId,
                        FullName = trainingPackage.Coach.User?.FullName ?? string.Empty,
                        AvatarUrl = trainingPackage.Coach.User?.AvatarUrl,
                        Headline = trainingPackage.Coach.Headline,
                        Bio = trainingPackage.Coach.Bio,
                        ExperienceYears = trainingPackage.Coach.ExperienceYears,
                        CoverImageUrl = trainingPackage.Coach.CoverImageUrl,
                        TeachingCity = trainingPackage.Coach.TeachingCity,
                        TeachingDistrict = trainingPackage.Coach.TeachingDistrict,
                        IsOnlineAvailable = trainingPackage.Coach.IsOnlineAvailable,
                        IsOfflineAvailable = trainingPackage.Coach.IsOfflineAvailable,
                        Specialties = trainingPackage.Coach.Specialties,
                        Rating = trainingPackage.Coach.Rating,
                        TotalReviews = trainingPackage.Coach.TotalReviews
                    }
            };
        }

        public static TrainingPackageResponse ToResponse(
            this TrainingPackage trainingPackage)
        {
            return new TrainingPackageResponse
            {
                Id = trainingPackage.Id,
                CoachId = trainingPackage.CoachId,
                SportId = trainingPackage.SportId,
                SportName = trainingPackage.Sport?.Name ?? string.Empty,
                Title = trainingPackage.Title,
                Description = trainingPackage.Description,
                Price = trainingPackage.Price,
                SessionCount = trainingPackage.SessionCount,
                DurationDays = trainingPackage.DurationDays,
                Location = trainingPackage.Location,
                IsOnline = trainingPackage.IsOnline,
                Level = trainingPackage.Level,
                GoalType = trainingPackage.GoalType,
                Status = trainingPackage.Status,
                RejectionReason = trainingPackage.RejectionReason,
                ReviewedAt = trainingPackage.ReviewedAt,
                ReviewedByUserId = trainingPackage.ReviewedByUserId,
                CreatedAt = trainingPackage.CreatedAt,
                UpdatedAt = trainingPackage.UpdatedAt
            };
        }
    }
}
