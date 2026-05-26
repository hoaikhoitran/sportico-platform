using SporticoApp.Application.DTOs.CoachPackages;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Mappings
{
    public static class CoachPackageMappingExtensions
    {
        public static CoachPackage ToPendingCoachPackage(
            this Package package,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new CoachPackage
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                PackageId = package.Id,

                // Với payOS, gói chưa active ngay.
                // Nhưng vẫn có thể set trước ngày bắt đầu/kết thúc.
                // Service chỉ cho dùng khi Status = active.
                StartDate = now,
                EndDate = now.AddDays(package.DurationDays),

                RemainingPosts = package.MaxPosts,
                Status = CoachPackageStatuses.Pending,
                CreatedAt = now
            };
        }

        public static CoachPackage ToActiveCoachPackage(
            this Package package,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new CoachPackage
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                PackageId = package.Id,
                StartDate = now,
                EndDate = now.AddDays(package.DurationDays),
                RemainingPosts = package.MaxPosts,
                Status = CoachPackageStatuses.Active,
                CreatedAt = now
            };
        }

        public static CoachPackageResponse ToResponse(
            this CoachPackage coachPackage)
        {
            return new CoachPackageResponse
            {
                Id = coachPackage.Id,
                CoachId = coachPackage.CoachId,
                PackageId = coachPackage.PackageId,
                PackageName = coachPackage.Package?.Name ?? string.Empty,
                StartDate = coachPackage.StartDate,
                EndDate = coachPackage.EndDate,
                RemainingPosts = coachPackage.RemainingPosts,
                Status = coachPackage.Status,
                CreatedAt = coachPackage.CreatedAt
            };
        }
    }
}