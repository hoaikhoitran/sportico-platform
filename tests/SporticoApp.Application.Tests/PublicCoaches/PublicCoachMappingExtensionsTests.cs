using SporticoApp.Application.Mappings;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.PublicCoaches;

public class PublicCoachMappingExtensionsTests
{
    [Fact]
    public void ToPublicDetailResponse_HealthyCoach_MapsAllRelations()
    {
        var coach = CoachProfileTestData.Healthy(CoachProfileTestData.FailingCoachId);

        var dto = coach.ToPublicDetailResponse();

        Assert.Equal(CoachProfileTestData.FailingCoachId, dto.CoachId);
        Assert.Equal("Phước Badminton", dto.FullName);
        Assert.Single(dto.Sports);
        Assert.Equal("Badminton", dto.Sports[0].Name);
        Assert.Single(dto.Media);
        Assert.Single(dto.TrainingPackages);
        Assert.Equal("Badminton", dto.TrainingPackages[0].SportName);
    }

    /// <summary>
    /// Direct regression for the production HTTP 500: a published package whose
    /// Sport navigation is null must not throw, and SportName falls back to "".
    /// </summary>
    [Fact]
    public void ToPublicDetailResponse_PublishedPackageWithNullSport_DoesNotThrow()
    {
        var coach = CoachProfileTestData.WithPublishedPackageMissingSport(
            CoachProfileTestData.FailingCoachId);

        var dto = coach.ToPublicDetailResponse();

        var package = Assert.Single(dto.TrainingPackages);
        Assert.Equal(string.Empty, package.SportName);
        Assert.Equal(99, package.SportId);
    }

    [Fact]
    public void ToPublicDetailResponse_EmptyRelations_ReturnsEmptyArrays()
    {
        var coach = CoachProfileTestData.WithEmptyRelations();

        var dto = coach.ToPublicDetailResponse();

        Assert.Empty(dto.Sports);
        Assert.Empty(dto.Media);
        Assert.Empty(dto.TrainingPackages);
    }

    [Fact]
    public void ToPublicDetailResponse_NullTeachingFields_AreNullNotThrown()
    {
        var coach = CoachProfileTestData.WithNullTeachingFieldsAndOrphanSport();

        var dto = coach.ToPublicDetailResponse();

        Assert.Null(dto.TeachingAddress);
        Assert.Null(dto.TeachingCity);
        Assert.Null(dto.TeachingDistrict);
    }

    /// <summary>
    /// An orphaned coach_sports row (Sport never loaded / hard-deleted) must be
    /// skipped rather than dereferenced.
    /// </summary>
    [Fact]
    public void ToPublicDetailResponse_OrphanedCoachSport_IsSkipped()
    {
        var coach = CoachProfileTestData.WithNullTeachingFieldsAndOrphanSport();

        var dto = coach.ToPublicDetailResponse();

        Assert.Empty(dto.Sports);
    }

    [Fact]
    public void ToPublicDetailResponse_InactiveSport_IsExcluded()
    {
        var coach = CoachProfileTestData.Healthy();
        coach.CoachSports.Single().Sport!.IsActive = false;

        var dto = coach.ToPublicDetailResponse();

        Assert.Empty(dto.Sports);
    }

    [Fact]
    public void ToPublicDetailResponse_IdentityMedia_IsNotExposed()
    {
        var coach = CoachProfileTestData.Healthy();
        coach.Media.Add(new CoachProfileMedia
        {
            Id = Guid.NewGuid(),
            CoachId = coach.UserId,
            MediaType = "identity",
            MediaUrl = "https://cdn/id-card.jpg",
            OrderIndex = 1
        });

        var dto = coach.ToPublicDetailResponse();

        Assert.DoesNotContain(dto.Media, m => m.MediaType == "identity");
    }

    [Fact]
    public void ToPublicDetailResponse_UnpublishedPackages_AreExcluded()
    {
        var coach = CoachProfileTestData.Healthy();
        coach.TrainingPackages.Add(new TrainingPackage
        {
            Id = Guid.NewGuid(),
            CoachId = coach.UserId,
            SportId = 1,
            Title = "Draft package",
            Status = TrainingPackageStatuses.Pending
        });

        var dto = coach.ToPublicDetailResponse();

        Assert.All(dto.TrainingPackages, p => Assert.Equal(TrainingPackageStatuses.Published, p.Status));
    }

    /// <summary>
    /// A coach surfaced by the list must also map cleanly in detail — both
    /// projections run off the same entity graph and neither may throw.
    /// </summary>
    [Fact]
    public void SummaryAndDetail_FromSameEntity_BothSucceed()
    {
        var coach = CoachProfileTestData.WithPublishedPackageMissingSport();

        var summary = coach.ToPublicSummaryResponse();
        var detail = coach.ToPublicDetailResponse();

        Assert.Equal(summary.CoachId, detail.CoachId);
        Assert.Equal(summary.FullName, detail.FullName);
    }
}
