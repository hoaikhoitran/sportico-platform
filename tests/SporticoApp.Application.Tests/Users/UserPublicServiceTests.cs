using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Users;

/// <summary>
/// Verifies that the public user endpoint returns a safe, minimal DTO and nothing more.
/// </summary>
public class UserPublicServiceTests
{
    private static UserPublicService Build(User? user)
        => new(new FakeUserRepo(user));

    private static User MakeUser(bool withCoach = false, bool withLearner = false) => new()
    {
        Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        Email = "private@example.com",
        FullName = "Coach Nguyen",
        Phone = "+84901234567",
        AvatarUrl = "https://cdn.example.com/avatar.jpg",
        DateOfBirth = new DateTime(1990, 1, 1),
        PasswordHash = "secret-hash",
        Status = "active",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        UserRoles = new List<UserRole>
        {
            new() { RoleId = 2, Role = new Role { Id = 2, Name = "coach" } }
        },
        CoachProfile = withCoach ? new CoachProfile
        {
            Headline = "Badminton expert",
            Bio = "10 years coaching",
            ExperienceYears = 10,
            CoverImageUrl = "https://cdn.example.com/cover.jpg",
            Rating = 4.8m,
            TotalReviews = 42
        } : null,
        LearnerProfile = withLearner ? new LearnerProfile
        {
            Goal = "Improve footwork"
        } : null
    };

    [Fact]
    public async Task GetById_ExistingUser_ReturnsPublicFields()
    {
        var user = MakeUser();
        var svc = Build(user);

        var result = await svc.GetByIdAsync(user.Id);

        Assert.True(result.IsSuccess);
        var dto = result.Data!;
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal("Coach Nguyen", dto.FullName);
        Assert.Equal("https://cdn.example.com/avatar.jpg", dto.AvatarUrl);
        Assert.Equal(new[] { "coach" }, dto.Roles);
    }

    [Fact]
    public async Task GetById_UnknownId_ThrowsNotFound()
    {
        var svc = Build(user: null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => svc.GetByIdAsync(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.UserNotFound, ex.Code);
    }

    [Fact]
    public async Task GetById_ResponseDoesNotExposeEmail()
    {
        var user = MakeUser();
        var svc = Build(user);

        var result = await svc.GetByIdAsync(user.Id);

        // PublicUserResponse must not have an Email property at all.
        var dto = result.Data!;
        var props = dto.GetType().GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("Email", props);
    }

    [Fact]
    public async Task GetById_ResponseDoesNotExposePhone()
    {
        var user = MakeUser();
        var svc = Build(user);
        var result = await svc.GetByIdAsync(user.Id);
        var props = result.Data!.GetType().GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("Phone", props);
    }

    [Fact]
    public async Task GetById_ResponseDoesNotExposeDateOfBirth()
    {
        var user = MakeUser();
        var svc = Build(user);
        var result = await svc.GetByIdAsync(user.Id);
        var props = result.Data!.GetType().GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("DateOfBirth", props);
    }

    [Fact]
    public async Task GetById_ResponseDoesNotExposePasswordHash()
    {
        var user = MakeUser();
        var svc = Build(user);
        var result = await svc.GetByIdAsync(user.Id);
        var props = result.Data!.GetType().GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("PasswordHash", props);
    }

    [Fact]
    public async Task GetById_WithCoachProfile_ReturnsCoachSummary()
    {
        var user = MakeUser(withCoach: true);
        var svc = Build(user);

        var result = await svc.GetByIdAsync(user.Id);

        var coach = result.Data!.CoachProfile;
        Assert.NotNull(coach);
        Assert.Equal("Badminton expert", coach!.Headline);
        Assert.Equal(10, coach.ExperienceYears);
        Assert.Equal(4.8m, coach.Rating);
        Assert.Equal(42, coach.TotalReviews);
    }

    [Fact]
    public async Task GetById_WithLearnerProfile_ReturnsLearnerSummary()
    {
        var user = MakeUser(withLearner: true);
        var svc = Build(user);

        var result = await svc.GetByIdAsync(user.Id);

        var learner = result.Data!.LearnerProfile;
        Assert.NotNull(learner);
        Assert.Equal("Improve footwork", learner!.Goal);
    }

    [Fact]
    public async Task GetById_NoProfiles_ReturnsNullProfiles()
    {
        var user = MakeUser();
        var svc = Build(user);

        var result = await svc.GetByIdAsync(user.Id);

        Assert.Null(result.Data!.CoachProfile);
        Assert.Null(result.Data.LearnerProfile);
    }

    [Fact]
    public async Task GetById_RolesReturnNamesOnly_NoIds()
    {
        var user = MakeUser();
        var svc = Build(user);

        var result = await svc.GetByIdAsync(user.Id);

        // Roles must be plain strings, never objects with Id properties.
        foreach (var role in result.Data!.Roles)
        {
            Assert.IsType<string>(role);
        }
    }

    // ── fakes ────────────────────────────────────────────────────────────────

    private sealed class FakeUserRepo : IUserRepository
    {
        private readonly User? _user;
        public FakeUserRepo(User? user) => _user = user;

        public Task<User?> GetByIdWithProfilesAndRolesAsync(Guid id)
            => Task.FromResult(_user != null && _user.Id == id ? _user : null);
        public Task<User?> GetByIdWithRolesAsync(Guid id)
            => Task.FromResult(_user != null && _user.Id == id ? _user : null);

        public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<User?> GetByEmailWithRolesAsync(string email) => throw new NotImplementedException();
        public Task AddAsync(User user) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(User user) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<User?> GetByVerificationTokenAsync(string token) => throw new NotImplementedException();
        public Task<User?> GetByPasswordResetTokenAsync(string token) => throw new NotImplementedException();
        public Task UpdateAsync(User user) => throw new NotImplementedException();
        public Task<User?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<User?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedForAdminAsync(AdminUserFilterRequest filter) => throw new NotImplementedException();
        public Task<User?> GetByIdForAdminUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<bool> ExistsByEmailAsync(string email) => throw new NotImplementedException();
    }
}
