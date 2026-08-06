using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;
using SporticoApp.Application.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Core.Enums;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Application.Tests.GoogleAuth;

/// <summary>
/// In-memory doubles for the Google sign-in tests. No test ever calls Google: the identity
/// provider is faked, so what is under test is Sportico's account resolution and linking rules.
/// </summary>
internal static class GoogleAuthTestData
{
    public const string Subject = "google-sub-1001";
    public const string Email = "learner@gmail.com";
    public static readonly Guid LearnerRoleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    public static GoogleIdentity Identity(
        string? subject = null,
        string? email = null,
        bool emailVerified = true,
        string? fullName = "Google User",
        string? avatarUrl = "https://lh3.googleusercontent.com/a/photo.jpg") => new()
        {
            Subject = subject ?? Subject,
            Email = email ?? Email,
            EmailVerified = emailVerified,
            FullName = fullName,
            AvatarUrl = avatarUrl
        };

    public static User ExistingUser(
        string email = Email,
        string status = "active",
        string? passwordHash = "$2a$11$existinghashvalue",
        string fullName = "Existing User",
        string? avatarUrl = null) => new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName,
            AvatarUrl = avatarUrl,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
}

internal sealed class FakeGoogleIdentityProvider : IGoogleIdentityProvider
{
    private readonly GoogleIdentity? _identity;
    private readonly Exception? _throw;

    public FakeGoogleIdentityProvider(GoogleIdentity identity) => _identity = identity;
    public FakeGoogleIdentityProvider(Exception toThrow) => _throw = toThrow;

    public string? LastToken { get; private set; }

    public Task<GoogleIdentity> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        LastToken = idToken;
        if (_throw != null) throw _throw;
        return Task.FromResult(_identity!);
    }
}

/// <summary>
/// In production every repository shares one <c>AppDbContext</c>, so a single SaveChanges commits
/// work queued through ANY of them. The fakes model that with a shared coordinator — without it a
/// test would wrongly conclude that user + role + external login are not written atomically.
/// </summary>
internal sealed class FakeSaveCoordinator
{
    private readonly List<Action> _flushes = new();
    private readonly List<Action> _discards = new();

    public void Register(Action flush, Action discard)
    {
        _flushes.Add(flush);
        _discards.Add(discard);
    }

    public void FlushAll()
    {
        foreach (var flush in _flushes) flush();
    }

    /// <summary>
    /// A failed SaveChanges rolls the whole transaction back, so pending inserts queued through
    /// any repository must be dropped — not silently committed.
    /// </summary>
    public void DiscardAll()
    {
        foreach (var discard in _discards) discard();
    }
}

internal sealed class FakeGoogleUserRepository : IUserRepository
{
    public readonly List<User> Users = new();
    public int SaveChangesCalls { get; private set; }
    public FakeSaveCoordinator? Coordinator { get; set; }

    /// <summary>Set to simulate a concurrent first login winning the unique-constraint race.</summary>
    public Func<User, Exception?>? OnSaveNewUser { get; set; }

    public FakeGoogleUserRepository(params User[] seed) => Users.AddRange(seed);

    private User? _pendingNew;

    public Task<User?> GetByEmailAsync(string email)
        => Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByEmailWithRolesAsync(string email)
        => Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByIdWithRolesAsync(Guid id)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task AddWithoutSaveAsync(User user)
    {
        _pendingNew = user;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveChangesCalls++;

        if (_pendingNew != null)
        {
            var failure = OnSaveNewUser?.Invoke(_pendingNew);
            if (failure != null)
            {
                // Transaction rollback: neither the user nor anything queued alongside it lands.
                _pendingNew = null;
                Coordinator?.DiscardAll();
                throw failure;
            }

            Users.Add(_pendingNew);
            _pendingNew = null;
        }

        // Shared DbContext semantics: this save also commits work queued via the other repos.
        Coordinator?.FlushAll();
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        if (!Users.Contains(user)) Users.Add(user);
        SaveChangesCalls++;
        Coordinator?.FlushAll();
        return Task.CompletedTask;
    }

    public Task AddAsync(User user) { Users.Add(user); return Task.CompletedTask; }
    public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    public Task<User?> GetByIdForUpdateAsync(Guid id) => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    public Task<User?> GetByIdWithProfilesAndRolesAsync(Guid id) => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    public Task<User?> GetByVerificationTokenAsync(string token) => throw new NotImplementedException();
    public Task<User?> GetByPasswordResetTokenAsync(string token) => throw new NotImplementedException();
    public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedForAdminAsync(AdminUserFilterRequest filter) => throw new NotImplementedException();
    public Task<User?> GetByIdForAdminUpdateAsync(Guid id) => throw new NotImplementedException();
    public Task<bool> ExistsByEmailAsync(string email) => Task.FromResult(Users.Any(u => u.Email == email));
}

internal sealed class FakeRoleRepository : IRoleRepository
{
    private readonly Role? _learner;

    public FakeRoleRepository(bool learnerExists = true)
        => _learner = learnerExists
            ? new Role { Id = 1, Name = RoleConstants.Learner }
            : null;

    public Task<Role?> GetByNameAsync(string name)
        => Task.FromResult(name == RoleConstants.Learner ? _learner : null);
}

internal sealed class FakeUserRoleRepository : IUserRoleRepository
{
    public readonly List<UserRole> Added = new();
    public Task AddAsync(UserRole userRole) { Added.Add(userRole); return Task.CompletedTask; }
    public Task AddWithoutSaveAsync(UserRole userRole) { Added.Add(userRole); return Task.CompletedTask; }
}

internal sealed class FakeUserExternalLoginRepository : IUserExternalLoginRepository
{
    public readonly List<UserExternalLogin> Links = new();
    public int SaveChangesCalls { get; private set; }

    public FakeUserExternalLoginRepository(params UserExternalLogin[] seed) => Links.AddRange(seed);

    private readonly List<UserExternalLogin> _pending = new();

    /// <summary>Flushes queued links, mirroring a shared-DbContext SaveChanges from any repository.</summary>
    public void Flush()
    {
        Links.AddRange(_pending);
        _pending.Clear();
    }

    /// <summary>Drops queued links, mirroring a transaction rollback.</summary>
    public void Discard() => _pending.Clear();

    public Task<UserExternalLogin?> GetByProviderSubjectAsync(string provider, string providerSubject)
        => Task.FromResult(Links.FirstOrDefault(x => x.Provider == provider && x.ProviderSubject == providerSubject));

    public Task<UserExternalLogin?> GetByUserAndProviderAsync(Guid userId, string provider)
        => Task.FromResult(Links.FirstOrDefault(x => x.UserId == userId && x.Provider == provider));

    public Task AddWithoutSaveAsync(UserExternalLogin externalLogin)
    {
        _pending.Add(externalLogin);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveChangesCalls++;
        Flush();
        return Task.CompletedTask;
    }
}

internal sealed class FakeAuthExchangeCodeRepository : IAuthExchangeCodeRepository
{
    public readonly List<AuthExchangeCode> Codes = new();
    public int DeleteExpiredCalls { get; private set; }

    public Task AddAsync(AuthExchangeCode code)
    {
        Codes.Add(code);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Mirrors the real single-statement conditional UPDATE: the unused+unexpired check and the
    /// UsedAt write happen together, so two callers can never both win.
    /// </summary>
    public Task<AuthExchangeCode?> ConsumeAsync(string codeHash, DateTime nowUtc)
    {
        lock (Codes)
        {
            var row = Codes.FirstOrDefault(x =>
                x.CodeHash == codeHash && x.UsedAt == null && x.ExpiresAt > nowUtc);

            if (row == null) return Task.FromResult<AuthExchangeCode?>(null);

            row.UsedAt = nowUtc;
            return Task.FromResult<AuthExchangeCode?>(row);
        }
    }

    public Task<AuthExchangeCode?> FindAnyAsync(string codeHash)
        => Task.FromResult(Codes.FirstOrDefault(x => x.CodeHash == codeHash));

    public Task DeleteExpiredAsync(DateTime nowUtc)
    {
        DeleteExpiredCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeJwtService : IJwtService
{
    public TokenResult GenerateAccessToken(User user) => new()
    {
        Token = "access-token-for-" + user.Id,
        ExpiresAt = DateTime.UtcNow.AddMinutes(15)
    };
}

internal sealed class FakeRefreshTokenService : IRefreshTokenService
{
    private int _counter;
    public string GenerateRefreshToken() => "refresh-token-" + Interlocked.Increment(ref _counter);
    public TimeSpan GetRefreshTokenLifetime() => TimeSpan.FromDays(30);
}

/// <summary>A validator that always passes — request-shape validation has its own tests.</summary>
internal sealed class PassThroughValidator<T> : AbstractValidator<T>
{
    public override ValidationResult Validate(ValidationContext<T> context) => new();
    public override Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken ct = default)
        => Task.FromResult(new ValidationResult());
}

internal static class GoogleAuthServiceBuilder
{
    public static (GoogleAuthService Service,
                   FakeGoogleUserRepository Users,
                   FakeUserExternalLoginRepository Links,
                   FakeUserRoleRepository UserRoles,
                   FakeAuthExchangeCodeRepository Codes)
        Build(
            GoogleIdentity? identity = null,
            Exception? providerThrows = null,
            User[]? seedUsers = null,
            UserExternalLogin[]? seedLinks = null,
            bool learnerRoleExists = true,
            string? clientId = "test-client-id.apps.googleusercontent.com")
    {
        var provider = providerThrows != null
            ? new FakeGoogleIdentityProvider(providerThrows)
            : new FakeGoogleIdentityProvider(identity ?? GoogleAuthTestData.Identity());

        var users = new FakeGoogleUserRepository(seedUsers ?? Array.Empty<User>());
        var links = new FakeUserExternalLoginRepository(seedLinks ?? Array.Empty<UserExternalLogin>());
        var userRoles = new FakeUserRoleRepository();
        var codes = new FakeAuthExchangeCodeRepository();

        // One save unit across repositories, exactly like the shared AppDbContext in production.
        var coordinator = new FakeSaveCoordinator();
        coordinator.Register(links.Flush, links.Discard);
        users.Coordinator = coordinator;

        // Fully qualified: "Options" also resolves to the SporticoApp.Application.Options namespace here.
        var options = Microsoft.Extensions.Options.Options.Create(new GoogleAuthOptions
        {
            ClientId = clientId,
            ClientSecret = "unused-in-unit-tests",
            CallbackUrl = "https://api.example.com/api/auth/google/callback",
            FrontendUrl = "https://app.example.com",
            ExchangeCodeLifetimeSeconds = 90
        });

        var tokenIssuer = new TokenIssuer(users, new FakeJwtService(), new FakeRefreshTokenService());

        var service = new GoogleAuthService(
            provider,
            users,
            new FakeRoleRepository(learnerRoleExists),
            userRoles,
            links,
            codes,
            tokenIssuer,
            options,
            new PassThroughValidator<GoogleIdTokenLoginRequest>(),
            new PassThroughValidator<GoogleExchangeCodeRequest>());

        return (service, users, links, userRoles, codes);
    }
}
