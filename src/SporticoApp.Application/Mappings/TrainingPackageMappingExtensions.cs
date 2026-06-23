using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Mappings
{
    public static class TrainingPackageMappingExtensions
    {
        /// <summary>
        /// Computes the legacy <c>DurationDays</c> from the new start/end-date model so existing
        /// booking-expiry logic keeps working. Always at least 1 day.
        /// </summary>
        private static int ComputeDurationDays(DateTime startDate, DateTime endDate)
        {
            var days = (endDate.Date - startDate.Date).Days + 1;
            return days < 1 ? 1 : days;
        }

        public static TrainingPackage ToEntity(
            this CreateTrainingPackageRequest request,
            Guid coachId)
        {
            var now = DateTime.UtcNow;
            var packageId = Guid.NewGuid();

            return new TrainingPackage
            {
                Id = packageId,
                CoachId = coachId,
                SportId = request.SportId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Price = request.Price,
                SessionCount = request.SessionCount,
                DurationDays = ComputeDurationDays(request.StartDate, request.EndDate),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Location = request.Location?.Trim(),
                IsOnline = request.IsOnline,
                Level = request.Level?.Trim(),
                GoalType = request.GoalType?.Trim(),
                Status = TrainingPackageStatuses.Pending,
                CreatedAt = now,
                UpdatedAt = now,
                SessionSlots = request.Sessions
                    .Select(s => s.ToSlotEntity(packageId, now))
                    .ToList()
            };
        }

        /// <summary>Maps one schedule input to a new package session slot entity (status = open).</summary>
        public static TrainingPackageSessionSlot ToSlotEntity(
            this CreateTrainingPackageSessionRequest request,
            Guid trainingPackageId,
            DateTime now)
        {
            return new TrainingPackageSessionSlot
            {
                Id = Guid.NewGuid(),
                TrainingPackageId = trainingPackageId,
                SessionNumber = request.SessionNumber,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Level = request.Level?.Trim(),
                Location = request.Location?.Trim(),
                IsOnline = request.IsOnline,
                MeetingUrl = request.MeetingUrl?.Trim(),
                Note = request.Note?.Trim(),
                MaxParticipants = request.MaxParticipants,
                BookedParticipants = 0,
                Status = TrainingPackageSessionSlotStatuses.Open,
                Version = 0,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        /// <summary>
        /// Applies scalar updates. The schedule (session slots) is replaced separately by the service
        /// because it manipulates the tracked child collection.
        /// </summary>
        public static void ApplyUpdate(
            this TrainingPackage trainingPackage,
            UpdateTrainingPackageRequest request)
        {
            trainingPackage.SportId = request.SportId;
            trainingPackage.Title = request.Title.Trim();
            trainingPackage.Description = request.Description?.Trim();
            trainingPackage.Price = request.Price;
            trainingPackage.SessionCount = request.SessionCount;
            trainingPackage.DurationDays = ComputeDurationDays(request.StartDate, request.EndDate);
            trainingPackage.StartDate = request.StartDate;
            trainingPackage.EndDate = request.EndDate;
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

        public static TrainingPackageSessionResponse ToResponse(
            this TrainingPackageSessionSlot slot)
        {
            var remaining = slot.MaxParticipants - slot.BookedParticipants;
            return new TrainingPackageSessionResponse
            {
                Id = slot.Id,
                SessionNumber = slot.SessionNumber,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Level = slot.Level,
                Location = slot.Location,
                IsOnline = slot.IsOnline,
                MeetingUrl = slot.MeetingUrl,
                Note = slot.Note,
                MaxParticipants = slot.MaxParticipants,
                BookedParticipants = slot.BookedParticipants,
                RemainingParticipants = remaining < 0 ? 0 : remaining,
                Status = slot.Status
            };
        }

        private static List<TrainingPackageSessionResponse> MapSessions(
            this TrainingPackage trainingPackage)
        {
            return trainingPackage.SessionSlots == null
                ? new List<TrainingPackageSessionResponse>()
                : trainingPackage.SessionSlots
                    .OrderBy(s => s.SessionNumber)
                    .Select(s => s.ToResponse())
                    .ToList();
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
                StartDate = trainingPackage.StartDate,
                EndDate = trainingPackage.EndDate,
                Location = trainingPackage.Location,
                IsOnline = trainingPackage.IsOnline,
                Level = trainingPackage.Level,
                GoalType = trainingPackage.GoalType,
                Status = trainingPackage.Status,
                CreatedAt = trainingPackage.CreatedAt,
                UpdatedAt = trainingPackage.UpdatedAt,
                Sessions = trainingPackage.MapSessions(),
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
                StartDate = trainingPackage.StartDate,
                EndDate = trainingPackage.EndDate,
                Location = trainingPackage.Location,
                IsOnline = trainingPackage.IsOnline,
                Level = trainingPackage.Level,
                GoalType = trainingPackage.GoalType,
                Status = trainingPackage.Status,
                RejectionReason = trainingPackage.RejectionReason,
                ReviewedAt = trainingPackage.ReviewedAt,
                ReviewedByUserId = trainingPackage.ReviewedByUserId,
                CreatedAt = trainingPackage.CreatedAt,
                UpdatedAt = trainingPackage.UpdatedAt,
                Sessions = trainingPackage.MapSessions()
            };
        }
    }
}
