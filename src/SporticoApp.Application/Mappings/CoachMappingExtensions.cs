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

        public static CoachProfileResponse ToResponse(
            this CoachProfile coachProfile)
        {
            return new CoachProfileResponse
            {
                UserId = coachProfile.UserId,

                Headline = coachProfile.Headline,

                Bio = coachProfile.Bio,

                ExperienceYears = coachProfile.ExperienceYears,

                Rating = coachProfile.Rating,

                TotalReviews = coachProfile.TotalReviews,

                CreatedAt = coachProfile.CreatedAt,

                UpdatedAt = coachProfile.UpdatedAt
            };
        }
    }
}
