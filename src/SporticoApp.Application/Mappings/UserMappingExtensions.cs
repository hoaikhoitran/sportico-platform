using System;
using System.Linq;
using SporticoApp.Application.DTOs.Users;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class UserMappingExtensions
    {
        public static CurrentUserResponse ToCurrentUserResponse(this User user)
        {
            return new CurrentUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                DateOfBirth = user.DateOfBirth,
                Status = user.Status,
                Roles = user.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role.Name)
                    .OrderBy(name => name)
                    .ToList(),
                CoachProfile = user.CoachProfile == null
                    ? null
                    : new CoachProfileSummaryResponse
                    {
                        Headline = user.CoachProfile.Headline,
                        Bio = user.CoachProfile.Bio,
                        ExperienceYears = user.CoachProfile.ExperienceYears,
                        CoverImageUrl = user.CoachProfile.CoverImageUrl,
                        Rating = user.CoachProfile.Rating,
                        TotalReviews = user.CoachProfile.TotalReviews
                    },
                LearnerProfile = user.LearnerProfile == null
                    ? null
                    : new LearnerProfileSummaryResponse
                    {
                        Goal = user.LearnerProfile.Goal
                    }
            };
        }

        public static AdminUserResponse ToAdminUserResponse(this User user)
        {
            return new AdminUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                DateOfBirth = user.DateOfBirth,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role.Name)
                    .OrderBy(name => name)
                    .ToList(),
                CoachProfile = user.CoachProfile == null
                    ? null
                    : new CoachProfileSummaryResponse
                    {
                        Headline = user.CoachProfile.Headline,
                        Bio = user.CoachProfile.Bio,
                        ExperienceYears = user.CoachProfile.ExperienceYears,
                        CoverImageUrl = user.CoachProfile.CoverImageUrl,
                        Rating = user.CoachProfile.Rating,
                        TotalReviews = user.CoachProfile.TotalReviews
                    },
                LearnerProfile = user.LearnerProfile == null
                    ? null
                    : new LearnerProfileSummaryResponse
                    {
                        Goal = user.LearnerProfile.Goal
                    }
            };
        }

        public static PublicUserResponse ToPublicUserResponse(this User user)
        {
            return new PublicUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Roles = user.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role.Name)
                    .OrderBy(name => name)
                    .ToList(),
                CoachProfile = user.CoachProfile == null
                    ? null
                    : new CoachProfileSummaryResponse
                    {
                        Headline = user.CoachProfile.Headline,
                        Bio = user.CoachProfile.Bio,
                        ExperienceYears = user.CoachProfile.ExperienceYears,
                        CoverImageUrl = user.CoachProfile.CoverImageUrl,
                        Rating = user.CoachProfile.Rating,
                        TotalReviews = user.CoachProfile.TotalReviews
                    },
                LearnerProfile = user.LearnerProfile == null
                    ? null
                    : new LearnerProfileSummaryResponse
                    {
                        Goal = user.LearnerProfile.Goal
                    }
            };
        }

        public static void ApplyUpdate(this User user, UpdateMeRequest request)
        {
            user.FullName = request.FullName.Trim();

            user.Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim();

            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl)
                ? null
                : request.AvatarUrl.Trim();

            user.DateOfBirth = request.DateOfBirth;

            user.UpdatedAt = DateTime.UtcNow;
        }
    }
}
