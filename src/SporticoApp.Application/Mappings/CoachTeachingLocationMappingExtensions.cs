using System;
using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class CoachTeachingLocationMappingExtensions
    {
        public static CoachTeachingLocation ToEntity(
            this CreateCoachTeachingLocationRequest request,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new CoachTeachingLocation
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                Address = request.Address.Trim(),
                City = Normalize(request.City),
                District = Normalize(request.District),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IsDefault = request.IsDefault,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public static void ApplyUpdate(
            this CoachTeachingLocation location,
            UpdateCoachTeachingLocationRequest request)
        {
            location.Address = request.Address.Trim();
            location.City = Normalize(request.City);
            location.District = Normalize(request.District);
            location.Latitude = request.Latitude;
            location.Longitude = request.Longitude;
            location.IsDefault = request.IsDefault;
            location.UpdatedAt = DateTime.UtcNow;
        }

        public static CoachTeachingLocationResponse ToResponse(
            this CoachTeachingLocation location)
        {
            return new CoachTeachingLocationResponse
            {
                Id = location.Id,
                CoachId = location.CoachId,
                Address = location.Address,
                City = location.City,
                District = location.District,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                IsDefault = location.IsDefault,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
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
