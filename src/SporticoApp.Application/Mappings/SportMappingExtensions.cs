using System;
using SporticoApp.Application.DTOs.Sports;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class SportMappingExtensions
    {
        public static Sport ToEntity(
            this CreateSportRequest request,
            string finalSlug)
        {
            var now = DateTime.UtcNow;

            return new Sport
            {
                Name = request.Name.Trim(),
                Slug = finalSlug,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                IconUrl = string.IsNullOrWhiteSpace(request.IconUrl)
                    ? null
                    : request.IconUrl.Trim(),
                IsActive = true,
                CreatedAt = now
            };
        }

        public static SportResponse ToResponse(
            this Sport sport)
        {
            return new SportResponse
            {
                Id = sport.Id,
                Name = sport.Name,
                Slug = sport.Slug,
                Description = sport.Description,
                IconUrl = sport.IconUrl,
                IsActive = sport.IsActive,
                CreatedAt = sport.CreatedAt
            };
        }

        public static List<SportResponse> ToResponseList(
            this IEnumerable<Sport> sports)
        {
            return sports
                .Select(x => x.ToResponse())
                .ToList();
        }
    }
}
