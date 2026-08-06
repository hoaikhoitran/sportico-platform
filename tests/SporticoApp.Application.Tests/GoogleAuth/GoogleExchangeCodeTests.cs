using System.Security.Cryptography;
using System.Text;
using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.GoogleAuth;

/// <summary>
/// Flow B step 2. The exchange code is the only thing that travels in a URL, so it must be
/// single-use, short-lived, and unrecoverable from the database.
/// </summary>
public class GoogleExchangeCodeTests
{
    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    // 1. Happy path: a fresh code yields real Sportico tokens.
    [Fact]
    public async Task ValidCode_ReturnsTokens()
    {
        var (svc, users, _, _, _) = GoogleAuthServiceBuilder.Build();
        var code = await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity());

        var result = await svc.ExchangeCodeAsync(new GoogleExchangeCodeRequest { Code = code });

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Data!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));
        Assert.True(result.Data.ExpiresAt > DateTime.UtcNow);
        Assert.Equal("access-token-for-" + users.Users[0].Id, result.Data.AccessToken);
    }

    // 2. The plaintext code is NEVER persisted — only its SHA-256 hash.
    [Fact]
    public async Task Code_IsStoredOnlyAsSha256Hash()
    {
        var (svc, _, _, _, codes) = GoogleAuthServiceBuilder.Build();

        var code = await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity());

        var stored = Assert.Single(codes.Codes);
        Assert.NotEqual(code, stored.CodeHash);
        Assert.Equal(Sha256Hex(code), stored.CodeHash);
        Assert.Equal(64, stored.CodeHash.Length);           // hex SHA-256
        Assert.DoesNotContain(code, stored.CodeHash);
    }

    // 3. A code works exactly once.
    [Fact]
    public async Task Code_CanBeUsedOnce_SecondUseIsRejected()
    {
        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build();
        var code = await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity());

        var first = await svc.ExchangeCodeAsync(new GoogleExchangeCodeRequest { Code = code });
        Assert.True(first.IsSuccess);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => svc.ExchangeCodeAsync(new GoogleExchangeCodeRequest { Code = code }));

        Assert.Equal(ErrorCodes.GoogleExchangeCodeAlreadyUsed, ex.Code);
    }

    // 4. An expired code is refused with its own distinct code.
    [Fact]
    public async Task ExpiredCode_IsRejected()
    {
        var (svc, _, _, _, codes) = GoogleAuthServiceBuilder.Build();
        var code = await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity());

        codes.Codes[0].ExpiresAt = DateTime.UtcNow.AddSeconds(-1);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.ExchangeCodeAsync(new GoogleExchangeCodeRequest { Code = code }));

        Assert.Equal(ErrorCodes.GoogleExchangeCodeExpired, ex.Code);
    }

    // 5. A code that was never issued is simply invalid.
    [Fact]
    public async Task UnknownCode_IsRejected()
    {
        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.ExchangeCodeAsync(new GoogleExchangeCodeRequest { Code = "never-issued-code" }));

        Assert.Equal(ErrorCodes.GoogleExchangeCodeInvalid, ex.Code);
    }

    // 6. Two concurrent consumes: exactly one wins, and the loser gets no token.
    [Fact]
    public async Task TwoConcurrentConsumes_OnlyOneSucceeds()
    {
        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build();
        var code = await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity());

        var attempts = Enumerable.Range(0, 2).Select(async _ =>
        {
            try
            {
                var r = await svc.ExchangeCodeAsync(new GoogleExchangeCodeRequest { Code = code });
                return r.IsSuccess;
            }
            catch (AppException)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(ok => ok));
    }

    // 7. The code resolves to the user it was minted for — not "some" user.
    [Fact]
    public async Task Code_BelongsToCorrectUser()
    {
        var other = GoogleAuthTestData.ExistingUser(email: "someone.else@gmail.com");
        var (svc, users, _, _, codes) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { other });

        var code = await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity());

        var googleUser = users.Users.Single(u => u.Email == GoogleAuthTestData.Email);
        Assert.Equal(googleUser.Id, Assert.Single(codes.Codes).UserId);

        var result = await svc.ExchangeCodeAsync(new GoogleExchangeCodeRequest { Code = code });
        Assert.Equal("access-token-for-" + googleUser.Id, result.Data!.AccessToken);
    }

    // 8. Codes are unguessable and unique per issue.
    [Fact]
    public async Task Codes_AreUniqueAndHighEntropy()
    {
        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build();

        var issued = new List<string>();
        for (var i = 0; i < 20; i++)
        {
            issued.Add(await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity()));
        }

        Assert.Equal(20, issued.Distinct().Count());
        // 32 random bytes base64url, no padding.
        Assert.All(issued, c => Assert.Equal(43, c.Length));
        Assert.All(issued, c => Assert.DoesNotContain('=', c));
        Assert.All(issued, c => Assert.DoesNotContain('+', c));
        Assert.All(issued, c => Assert.DoesNotContain('/', c));
    }

    // 9. No token material is ever written into the code row.
    [Fact]
    public async Task CodeRow_NeverStoresTokens()
    {
        var (svc, _, _, _, codes) = GoogleAuthServiceBuilder.Build();
        await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity());

        var row = Assert.Single(codes.Codes);
        // The entity has exactly these fields — no access/refresh token anywhere.
        Assert.NotEqual(Guid.Empty, row.Id);
        Assert.NotEqual(Guid.Empty, row.UserId);
        Assert.False(string.IsNullOrWhiteSpace(row.CodeHash));
        Assert.Null(row.UsedAt);
        Assert.True(row.ExpiresAt > DateTime.UtcNow);
    }

    // 10. Issuing a code triggers opportunistic cleanup, so no worker is needed.
    [Fact]
    public async Task IssuingCode_TriggersOpportunisticCleanup()
    {
        var (svc, _, _, _, codes) = GoogleAuthServiceBuilder.Build();

        await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity());

        Assert.Equal(1, codes.DeleteExpiredCalls);
    }

    // 11. The redirect flow creates/links accounts exactly like the ID-token flow.
    [Fact]
    public async Task RedirectFlow_CreatesActiveLearner_LikeIdTokenFlow()
    {
        var (svc, users, links, userRoles, _) = GoogleAuthServiceBuilder.Build();

        await svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity());

        var created = Assert.Single(users.Users);
        Assert.Equal("active", created.Status);
        Assert.Null(created.PasswordHash);
        Assert.Single(userRoles.Added);
        Assert.Single(links.Links);
    }

    // 12. A banned user cannot obtain a code through the redirect flow either.
    [Fact]
    public async Task RedirectFlow_BannedUser_IsRejectedBeforeCodeIsIssued()
    {
        var banned = GoogleAuthTestData.ExistingUser(status: "banned");
        var (svc, _, _, _, codes) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { banned });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => svc.CreateExchangeCodeForIdentityAsync(GoogleAuthTestData.Identity()));

        Assert.Empty(codes.Codes);
    }
}
