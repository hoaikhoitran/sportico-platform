using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Tests.PublicCoaches;

/// <summary>
/// Builds in-memory <see cref="CoachProfile"/> graphs that mirror what the
/// public-coach repository returns (an active user plus optional related data),
/// so mapper / service behaviour can be exercised without a database.
/// </summary>
internal static class CoachProfileTestData
{
    /// <summary>The coach that 500'd in production (Phước Badminton).</summary>
    public static readonly Guid FailingCoachId =
        Guid.Parse("07a775ae-0d89-4ef7-8a9f-582b64f2cc55");

    /// <summary>
    /// A fully-populated, healthy coach: active user, one active sport,
    /// gallery media and a published package whose Sport navigation is loaded.
    /// </summary>
    public static CoachProfile Healthy(Guid? coachId = null)
    {
        var id = coachId ?? Guid.NewGuid();
        var badminton = new Sport { Id = 1, Name = "Badminton", Slug = "badminton", IsActive = true };

        return new CoachProfile
        {
            UserId = id,
            User = new User { Id = id, FullName = "Phước Badminton", Status = "active" },
            Headline = "Pro badminton coach",
            Bio = "10 years experience",
            TeachingCity = "Hà Nội",
            TeachingDistrict = "Cầu Giấy",
            TeachingAddress = "123 Đường ABC",
            IsOnlineAvailable = true,
            IsOfflineAvailable = true,
            Rating = 4.8m,
            TotalReviews = 12,
            CoachSports = new List<CoachSport>
            {
                new() { CoachId = id, SportId = badminton.Id, Sport = badminton }
            },
            Media = new List<CoachProfileMedia>
            {
                new()
                {
                    Id = Guid.NewGuid(), CoachId = id, MediaType = "gallery",
                    MediaUrl = "https://cdn/img1.jpg", OrderIndex = 0
                }
            },
            TrainingPackages = new List<TrainingPackage>
            {
                new()
                {
                    Id = Guid.NewGuid(), CoachId = id, SportId = badminton.Id, Sport = badminton,
                    Title = "Beginner course", Price = 1000000m, SessionCount = 8,
                    DurationDays = 30, Status = TrainingPackageStatuses.Published
                }
            }
        };
    }

    /// <summary>
    /// The production-bug shape: a published training package whose <c>Sport</c>
    /// navigation was never loaded (null). The old mapper dereferenced
    /// <c>x.Sport.Name</c> here and threw, producing the HTTP 500.
    /// </summary>
    public static CoachProfile WithPublishedPackageMissingSport(Guid? coachId = null)
    {
        var coach = Healthy(coachId);
        coach.TrainingPackages = new List<TrainingPackage>
        {
            new()
            {
                Id = Guid.NewGuid(), CoachId = coach.UserId, SportId = 99,
                Sport = null!, // not Include()'d -> null at runtime
                Title = "Orphan-sport package", Price = 500000m, SessionCount = 4,
                DurationDays = 14, Status = TrainingPackageStatuses.Published
            }
        };
        return coach;
    }

    /// <summary>
    /// An active coach with no media, no packages and no sports — every optional
    /// relation empty.
    /// </summary>
    public static CoachProfile WithEmptyRelations(Guid? coachId = null)
    {
        var id = coachId ?? Guid.NewGuid();
        return new CoachProfile
        {
            UserId = id,
            User = new User { Id = id, FullName = "Bare Coach", Status = "active" },
            CoachSports = new List<CoachSport>(),
            Media = new List<CoachProfileMedia>(),
            TrainingPackages = new List<TrainingPackage>()
        };
    }

    /// <summary>
    /// A coach whose teaching address/city/district are all null and whose
    /// coach-sport row points at a sport that was never loaded (orphan).
    /// </summary>
    public static CoachProfile WithNullTeachingFieldsAndOrphanSport(Guid? coachId = null)
    {
        var id = coachId ?? Guid.NewGuid();
        return new CoachProfile
        {
            UserId = id,
            User = new User { Id = id, FullName = "No Address Coach", Status = "active" },
            TeachingAddress = null,
            TeachingCity = null,
            TeachingDistrict = null,
            Specialties = null,
            Headline = null,
            Bio = null,
            CoachSports = new List<CoachSport>
            {
                new() { CoachId = id, SportId = 42, Sport = null! } // orphaned link
            },
            Media = new List<CoachProfileMedia>(),
            TrainingPackages = new List<TrainingPackage>()
        };
    }
}
