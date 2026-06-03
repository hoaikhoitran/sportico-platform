using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Users;
using SporticoApp.Core.Entities;
using SporticoApp.Infrastructure.Persistence;
using SporticoApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SporticoApp.Application.Tests.Users;

/// <summary>
/// Admin paged-list filtering against the real EF model (InMemory). The case-insensitive
/// search uses PostgreSQL ILIKE (not InMemory-translatable), so these tests exercise the
/// role/status filters, ordering and pagination paths.
/// </summary>
public class AdminUserRepositoryTests
{
    private static AppDbContext NewContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static User MakeUser(string email, string status, DateTime created, params int[] roleIds)
    {
        var u = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = email.Split('@')[0],
            PasswordHash = "x",
            Status = status,
            CreatedAt = created,
            UpdatedAt = created,
            UserRoles = new List<UserRole>()
        };
        foreach (var id in roleIds)
            u.UserRoles.Add(new UserRole { UserId = u.Id, RoleId = id }); // Role linked via seeded Role rows
        return u;
    }

    private static async Task<AppDbContext> SeededAsync()
    {
        var ctx = NewContext();
        var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // Seed the role rows once (shared keys).
        ctx.Roles.AddRange(
            new Role { Id = 1, Name = "learner", CreatedAt = t },
            new Role { Id = 2, Name = "coach", CreatedAt = t },
            new Role { Id = 3, Name = "admin", CreatedAt = t });
        ctx.Users.AddRange(
            MakeUser("learner@t.io", "active", t.AddDays(1), 1),
            MakeUser("coach@t.io", "active", t.AddDays(2), 2),
            MakeUser("admin@t.io", "banned", t.AddDays(3), 2, 3));
        await ctx.SaveChangesAsync();
        return ctx;
    }

    [Fact]
    public async Task GetPaged_FilterByRole_ReturnsMatching_NewestFirst()
    {
        await using var ctx = await SeededAsync();
        var repo = new UserRepository(ctx);

        var (items, total) = await repo.GetPagedForAdminAsync(
            new AdminUserFilterRequest { Role = "coach", PageNumber = 1, PageSize = 10 });

        Assert.Equal(2, total);
        Assert.Equal(new[] { "admin@t.io", "coach@t.io" }, items.Select(u => u.Email).ToArray()); // newest first
    }

    [Fact]
    public async Task GetPaged_FilterByStatus_ReturnsMatching()
    {
        await using var ctx = await SeededAsync();
        var repo = new UserRepository(ctx);

        var (items, total) = await repo.GetPagedForAdminAsync(
            new AdminUserFilterRequest { Status = "banned", PageNumber = 1, PageSize = 10 });

        Assert.Equal(1, total);
        Assert.Equal("admin@t.io", items[0].Email);
    }

    [Fact]
    public async Task GetPaged_Paginates()
    {
        await using var ctx = await SeededAsync();
        var repo = new UserRepository(ctx);

        var page1 = await repo.GetPagedForAdminAsync(new AdminUserFilterRequest { PageNumber = 1, PageSize = 2 });
        var page2 = await repo.GetPagedForAdminAsync(new AdminUserFilterRequest { PageNumber = 2, PageSize = 2 });

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(3, page2.TotalCount);
        Assert.Single(page2.Items);
    }

    [Fact]
    public async Task GetPaged_IncludesRoles()
    {
        await using var ctx = await SeededAsync();
        var repo = new UserRepository(ctx);

        var (items, _) = await repo.GetPagedForAdminAsync(
            new AdminUserFilterRequest { Status = "banned", PageNumber = 1, PageSize = 10 });

        var roles = items[0].UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "admin", "coach" }, roles);
    }
}
