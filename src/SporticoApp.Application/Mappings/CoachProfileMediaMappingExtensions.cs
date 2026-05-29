using System;
using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class CoachProfileMediaMappingExtensions
    {
        public static CoachProfileMedia ToEntity(
            this CreateCoachProfileMediaRequest request,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new CoachProfileMedia
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                MediaType = request.MediaType.Trim().ToLowerInvariant(),
                MediaUrl = request.MediaUrl.Trim(),
                Title = Normalize(request.Title),
                Description = Normalize(request.Description),
                OrderIndex = request.OrderIndex,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public static void ApplyUpdate(
            this CoachProfileMedia media,
            UpdateCoachProfileMediaRequest request)
        {
            media.MediaType = request.MediaType.Trim().ToLowerInvariant();
            media.MediaUrl = request.MediaUrl.Trim();
            media.Title = Normalize(request.Title);
            media.Description = Normalize(request.Description);
            media.OrderIndex = request.OrderIndex;
            media.UpdatedAt = DateTime.UtcNow;
        }

        public static CoachProfileMediaResponse ToResponse(
            this CoachProfileMedia media)
        {
            return new CoachProfileMediaResponse
            {
                Id = media.Id,
                CoachId = media.CoachId,
                MediaType = media.MediaType,
                MediaUrl = media.MediaUrl,
                Title = media.Title,
                Description = media.Description,
                OrderIndex = media.OrderIndex,
                CreatedAt = media.CreatedAt,
                UpdatedAt = media.UpdatedAt
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
