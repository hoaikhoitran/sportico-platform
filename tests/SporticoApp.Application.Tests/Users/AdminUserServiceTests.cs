using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Users;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Helpers;
using Xunit;

namespace SporticoApp.Application.Tests.Users;

/// <summary>Admin user CRUD orchestration: uniqueness, role validation, hashing, role replacement, soft-delete.</summary>
public class AdminUserServiceTests
{
    private static AdminUserService Build(FakeUserRepo users, FakeRoleRepo roles, FakeUserRoleRepo userRoles)
        => new(
            users, roles, userRoles,
            new AdminUserFilterRequestValidator(),
            new AdminCreateUserRequestValidator(),
            new AdminUpdateUserRequestValidator());

    private static AdminCreateUserRequest ValidCreate() => new()
    {
        Email = "New@Test.IO",
        FullName = "New User",
        Password = "password123",
        Status = "active",
        Roles = new() { "learner" }
    };

    [Fact]
    public async Task Create_Valid_HashesPassword_NormalizesEmail_AddsRoles()
    {
        var users = new FakeUserRepo { EmailExists = false };
        var roles = new FakeRoleRepo { Map = { ["learner"] = 3 } };
        var userRoles = new FakeUserRoleRepo();
        var svc = Build(users, roles, userRoles);

        var result = await svc.CreateAsync(ValidCreate());

        Assert.True(result.IsSuccess);
        var created = Assert.Single(users.Added);
        Assert.Equal("new@test.io", created.Email);                 // normalized
        Assert.NotEqual("password123", created.PasswordHash);       // not plain text
        Assert.True(PasswordHelper.VerifyPassword("password123", created.PasswordHash));
        var ur = Assert.Single(userRoles.Added);
        Assert.Equal(3, ur.RoleId);
        Assert.Equal(created.Id, ur.UserId);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ThrowsConflict()
    {
        var users = new FakeUserRepo { EmailExists = true };
        var svc = Build(users, new FakeRoleRepo { Map = { ["learner"] = 3 } }, new FakeUserRoleRepo());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => svc.CreateAsync(ValidCreate()));
        Assert.Equal(ErrorCodes.EmailAlreadyExists, ex.Code);
        Assert.Empty(users.Added);
    }

    [Fact]
    public async Task Create_UnknownRole_ThrowsNotFound()
    {
        var users = new FakeUserRepo { EmailExists = false };
        var svc = Build(users, new FakeRoleRepo(), new FakeUserRoleRepo()); // no roles known

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => svc.CreateAsync(ValidCreate()));
        Assert.Equal(ErrorCodes.RoleNotFound, ex.Code);
        Assert.Empty(users.Added); // role checked before user creation
    }

    [Fact]
    public async Task Create_InvalidStatus_ThrowsValidation()
    {
        var req = ValidCreate();
        req.Status = "superuser";
        var svc = Build(new FakeUserRepo { EmailExists = false }, new FakeRoleRepo { Map = { ["learner"] = 3 } }, new FakeUserRoleRepo());

        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(req));
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFound()
    {
        var svc = Build(new FakeUserRepo(), new FakeRoleRepo(), new FakeUserRoleRepo());
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
        Assert.Equal(ErrorCodes.UserNotFound, ex.Code);
    }

    [Fact]
    public async Task Update_NotFound_ThrowsNotFound()
    {
        var svc = Build(new FakeUserRepo(), new FakeRoleRepo(), new FakeUserRoleRepo());
        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateAsync(Guid.NewGuid(), new AdminUpdateUserRequest { FullName = "Valid Name", Status = "active" }));
    }

    [Fact]
    public async Task Update_ReplaceRoles_DiffsCollection()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "u@t.io", FullName = "U", Status = "active",
            PasswordHash = "h", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            UserRoles = new List<UserRole>
            {
                new() { RoleId = 1, Role = new Role { Id = 1, Name = "learner" } },
                new() { RoleId = 2, Role = new Role { Id = 2, Name = "coach" } }
            }
        };
        var users = new FakeUserRepo { ForAdminUpdate = user };
        var roles = new FakeRoleRepo { Map = { ["coach"] = 2, ["admin"] = 5 } };
        var svc = Build(users, roles, new FakeUserRoleRepo());

        var result = await svc.UpdateAsync(user.Id, new AdminUpdateUserRequest
        {
            FullName = "Updated", Status = "active", Roles = new() { "coach", "admin" }
        });

        Assert.True(result.IsSuccess);
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).OrderBy(x => x).ToList();
        Assert.Equal(new[] { 2, 5 }, roleIds);   // kept 2, removed 1, added 5
        Assert.Equal("Updated", user.FullName);
    }

    [Fact]
    public async Task Update_NullRoles_KeepsRolesUnchanged()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "u@t.io", FullName = "U", Status = "active",
            PasswordHash = "h", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            UserRoles = new List<UserRole> { new() { RoleId = 1, Role = new Role { Id = 1, Name = "learner" } } }
        };
        var svc = Build(new FakeUserRepo { ForAdminUpdate = user }, new FakeRoleRepo(), new FakeUserRoleRepo());

        await svc.UpdateAsync(user.Id, new AdminUpdateUserRequest { FullName = "U2", Status = "active", Roles = null });

        Assert.Single(user.UserRoles);
        Assert.Equal(1, user.UserRoles.First().RoleId);
    }

    [Fact]
    public async Task Delete_SetsStatusInactive_NoPhysicalDelete()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "u@t.io", FullName = "U", Status = "active",
            PasswordHash = "h", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            UserRoles = new List<UserRole>()
        };
        var svc = Build(new FakeUserRepo { ForAdminUpdate = user }, new FakeRoleRepo(), new FakeUserRoleRepo());

        var result = await svc.DeleteAsync(user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("inactive", user.Status);
    }

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeUserRepo : IUserRepository
    {
        public bool EmailExists;
        public readonly List<User> Added = new();
        public User? ForAdminUpdate;

        public Task<bool> ExistsByEmailAsync(string email) => Task.FromResult(EmailExists);
        public Task AddWithoutSaveAsync(User user) { Added.Add(user); return Task.CompletedTask; }
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<User?> GetByIdForAdminUpdateAsync(Guid id)
            => Task.FromResult(ForAdminUpdate != null && ForAdminUpdate.Id == id ? ForAdminUpdate : null);
        public Task<User?> GetByIdWithProfilesAndRolesAsync(Guid id)
            => Task.FromResult(ForAdminUpdate ?? Added.FirstOrDefault(u => u.Id == id));
        public Task<User?> GetByIdWithRolesAsync(Guid id)
            => Task.FromResult(ForAdminUpdate ?? Added.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<User?> GetByEmailWithRolesAsync(string email) => throw new NotImplementedException();
        public Task AddAsync(User user) => throw new NotImplementedException();
        public Task<User?> GetByVerificationTokenAsync(string token) => throw new NotImplementedException();
        public Task<User?> GetByPasswordResetTokenAsync(string token) => throw new NotImplementedException();
        public Task UpdateAsync(User user) => throw new NotImplementedException();
        public Task<User?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<User?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedForAdminAsync(AdminUserFilterRequest filter) => throw new NotImplementedException();
    }

    private sealed class FakeRoleRepo : IRoleRepository
    {
        public readonly Dictionary<string, int> Map = new();
        public Task<Role?> GetByNameAsync(string name)
            => Task.FromResult(Map.TryGetValue(name, out var id) ? new Role { Id = id, Name = name } : null);
    }

    private sealed class FakeUserRoleRepo : IUserRoleRepository
    {
        public readonly List<UserRole> Added = new();
        public Task AddAsync(UserRole userRole) { Added.Add(userRole); return Task.CompletedTask; }
        public Task AddWithoutSaveAsync(UserRole userRole) { Added.Add(userRole); return Task.CompletedTask; }
    }
}
