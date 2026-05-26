using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SporticoApp.Application.DTOs.Packages;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class PackageMappingExtensions
    {
        public static Package ToEntity(this CreatePackageRequest request)
        {
            return new Package
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                DurationDays = request.DurationDays,
                MaxPosts = request.MaxPosts,
                Price = request.Price,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static void ApplyUpdate(
            this Package package,
            UpdatePackageRequest request)
        {
            package.Name = request.Name.Trim();
            package.Description = request.Description?.Trim();
            package.DurationDays = request.DurationDays;
            package.MaxPosts = request.MaxPosts;
            package.Price = request.Price;
        }

        public static PackageResponse ToResponse(this Package package)
        {
            return new PackageResponse
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                DurationDays = package.DurationDays,
                MaxPosts = package.MaxPosts,
                Price = package.Price,
                IsActive = package.IsActive,
                CreatedAt = package.CreatedAt
            };
        }

        public static List<PackageResponse> ToResponseList(
            this IEnumerable<Package> packages)
        {
            return packages
                .Select(ToResponse)
                .ToList();
        }
    }
}
