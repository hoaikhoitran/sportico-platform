using Microsoft.Extensions.Logging.Abstractions;
using SporticoApp.Application.DTOs.PublicCoaches;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Enums;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.PublicCoaches;

public class PublicCoachServiceTests
{
    private static PublicCoachService CreateService(CoachProfile? coach)
        => new(new FakePublicCoachRepository(coach), NullLogger<PublicCoachService>.Instance);

    /// <summary>
    /// End-to-end (service layer) regression for the reported 500: the failing
    /// coach with a published package that has a null Sport must return success.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_FailingCoachWithNullPackageSport_ReturnsSuccess()
    {
        var coach = CoachProfileTestData.WithPublishedPackageMissingSport(
            CoachProfileTestData.FailingCoachId);
        var service = CreateService(coach);

        var result = await service.GetByIdAsync(CoachProfileTestData.FailingCoachId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(CoachProfileTestData.FailingCoachId, result.Data!.CoachId);
    }

    [Fact]
    public async Task GetByIdAsync_HealthyCoach_ReturnsSuccessWithData()
    {
        var coach = CoachProfileTestData.Healthy(CoachProfileTestData.FailingCoachId);
        var service = CreateService(coach);

        var result = await service.GetByIdAsync(CoachProfileTestData.FailingCoachId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Phước Badminton", result.Data!.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyRelations_ReturnsSuccessWithEmptyArrays()
    {
        var coach = CoachProfileTestData.WithEmptyRelations();
        var service = CreateService(coach);

        var result = await service.GetByIdAsync(coach.UserId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!.Sports);
        Assert.Empty(result.Data.Media);
        Assert.Empty(result.Data.TrainingPackages);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownCoach_ThrowsNotFound()
    {
        var service = CreateService(coach: null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetByIdAsync(Guid.NewGuid()));

        Assert.Equal(ErrorType.NotFound, ex.Type);
    }

    private sealed class FakePublicCoachRepository : IPublicCoachRepository
    {
        private readonly CoachProfile? _coach;

        public FakePublicCoachRepository(CoachProfile? coach) => _coach = coach;

        public Task<CoachProfile?> GetByIdAsync(Guid coachId)
            => Task.FromResult(_coach is not null && _coach.UserId == coachId ? _coach : null);

        public Task<(List<CoachProfile> Items, int TotalCount)> GetPagedAsync(
            PublicCoachFilterRequest filter)
        {
            var items = _coach is null ? new List<CoachProfile>() : new List<CoachProfile> { _coach };
            return Task.FromResult((items, items.Count));
        }
    }
}
