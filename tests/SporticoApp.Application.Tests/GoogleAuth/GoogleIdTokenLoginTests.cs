using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.GoogleAuth;

/// <summary>
/// Flow A — POST /api/auth/google. Identity always arrives already verified, so these tests are
/// about what Sportico does with it: create, link, reject, and never overwrite user-owned data.
/// </summary>
public class GoogleIdTokenLoginTests
{
    private static GoogleIdTokenLoginRequest Req(string token = "any-google-id-token") => new() { IdToken = token };

    // 1. First sign-in with no matching account creates an ACTIVE learner with no password.
    [Fact]
    public async Task VerifiedToken_NoExistingAccount_CreatesActiveLearner()
    {
        var (svc, users, links, userRoles, _) = GoogleAuthServiceBuilder.Build();

        var result = await svc.LoginWithIdTokenAsync(Req());

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Data!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));

        var created = Assert.Single(users.Users);
        Assert.Equal(GoogleAuthTestData.Email, created.Email);
        Assert.Equal("active", created.Status);
        Assert.Null(created.PasswordHash);                 // Google-only account
        Assert.Null(created.EmailVerificationToken);       // no verification email needed

        Assert.Single(userRoles.Added);                    // exactly the learner role
        var link = Assert.Single(links.Links);
        Assert.Equal(ExternalAuthProviders.Google, link.Provider);
        Assert.Equal(GoogleAuthTestData.Subject, link.ProviderSubject);
    }

    // 2. An existing local account with the same (Google-verified) email is LINKED, not duplicated.
    [Fact]
    public async Task VerifiedToken_ExistingActiveLocalUser_IsLinkedNotDuplicated()
    {
        var existing = GoogleAuthTestData.ExistingUser();
        var (svc, users, links, _, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { existing });

        var result = await svc.LoginWithIdTokenAsync(Req());

        Assert.True(result.IsSuccess);
        Assert.Single(users.Users);                        // no second account
        var link = Assert.Single(links.Links);
        Assert.Equal(existing.Id, link.UserId);
        Assert.Equal("$2a$11$existinghashvalue", existing.PasswordHash); // local password untouched
    }

    // 3. Google has verified the address, so an unverified (inactive) account becomes active.
    [Fact]
    public async Task VerifiedToken_InactiveUser_IsActivatedAndTokenCleared()
    {
        var existing = GoogleAuthTestData.ExistingUser(status: "inactive");
        existing.EmailVerificationToken = "pending-verify-token";

        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { existing });

        var result = await svc.LoginWithIdTokenAsync(Req());

        Assert.True(result.IsSuccess);
        Assert.Equal("active", existing.Status);
        Assert.Null(existing.EmailVerificationToken);
    }

    // 4. A ban must survive Google sign-in: no login, no activation, no new account.
    [Fact]
    public async Task VerifiedToken_BannedUser_IsRejected()
    {
        var banned = GoogleAuthTestData.ExistingUser(status: "banned");
        var (svc, users, links, _, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { banned });

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => svc.LoginWithIdTokenAsync(Req()));

        Assert.Equal(ErrorCodes.AccountNotActive, ex.Code);
        Assert.Equal("banned", banned.Status);
        Assert.Single(users.Users);
        Assert.Empty(links.Links);   // never linked
    }

    // 5. A "pending" account is a moderation state, not an email state — do not auto-activate it.
    [Fact]
    public async Task VerifiedToken_PendingUser_IsNotAutoActivated()
    {
        var pending = GoogleAuthTestData.ExistingUser(status: "pending");
        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { pending });

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => svc.LoginWithIdTokenAsync(Req()));

        Assert.Equal(ErrorCodes.AccountNotActive, ex.Code);
        Assert.Equal("pending", pending.Status);
    }

    // 6. Whatever the provider rejects (bad signature, wrong audience, expired) surfaces as 401.
    [Fact]
    public async Task InvalidToken_IsRejectedWithGoogleInvalidToken()
    {
        var (svc, users, _, _, _) = GoogleAuthServiceBuilder.Build(
            providerThrows: new UnauthorizedException(ErrorCodes.GoogleInvalidToken, "Google authentication failed"));

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => svc.LoginWithIdTokenAsync(Req()));

        Assert.Equal(ErrorCodes.GoogleInvalidToken, ex.Code);
        Assert.Empty(users.Users);   // nothing is created for an invalid token
    }

    // 7. An unverified Google email must never take over a Sportico account on the same address.
    [Fact]
    public async Task UnverifiedEmail_IsRejected()
    {
        var (svc, users, links, _, _) = GoogleAuthServiceBuilder.Build(
            identity: GoogleAuthTestData.Identity(emailVerified: false));

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => svc.LoginWithIdTokenAsync(Req()));

        Assert.Equal(ErrorCodes.GoogleEmailNotVerified, ex.Code);
        Assert.Empty(users.Users);
        Assert.Empty(links.Links);
    }

    // 8. Without "sub" there is no stable identifier — refuse rather than fall back to email.
    [Fact]
    public async Task MissingSubject_IsRejected()
    {
        var (svc, users, _, _, _) = GoogleAuthServiceBuilder.Build(
            identity: GoogleAuthTestData.Identity(subject: ""));

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => svc.LoginWithIdTokenAsync(Req()));

        Assert.Equal(ErrorCodes.GoogleInvalidToken, ex.Code);
        Assert.Empty(users.Users);
    }

    [Fact]
    public async Task MissingEmail_IsRejected()
    {
        var (svc, users, _, _, _) = GoogleAuthServiceBuilder.Build(
            identity: GoogleAuthTestData.Identity(email: ""));

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => svc.LoginWithIdTokenAsync(Req()));

        Assert.Equal(ErrorCodes.GoogleInvalidToken, ex.Code);
        Assert.Empty(users.Users);
    }

    // 9. A broken picture URL is cosmetic — it must never cost the user their login.
    [Theory]
    [InlineData("not-a-url")]
    [InlineData("http://insecure.example.com/a.jpg")]   // non-HTTPS is dropped
    [InlineData("javascript:alert(1)")]
    public async Task InvalidAvatarUrl_DoesNotFailLogin_AndIsNotStored(string avatar)
    {
        var (svc, users, _, _, _) = GoogleAuthServiceBuilder.Build(
            identity: GoogleAuthTestData.Identity(avatarUrl: avatar));

        var result = await svc.LoginWithIdTokenAsync(Req());

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(users.Users).AvatarUrl);
    }

    [Fact]
    public async Task ValidHttpsAvatar_IsStoredOnNewUser()
    {
        var (svc, users, _, _, _) = GoogleAuthServiceBuilder.Build();

        await svc.LoginWithIdTokenAsync(Req());

        Assert.StartsWith("https://lh3.googleusercontent.com/", Assert.Single(users.Users).AvatarUrl);
    }

    // 10. Profile data the user has already set is theirs; Google must not overwrite it.
    [Fact]
    public async Task ExistingCustomAvatar_IsNotOverwritten()
    {
        var existing = GoogleAuthTestData.ExistingUser(avatarUrl: "https://cdn.sportico.example/my-own.png");
        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { existing });

        await svc.LoginWithIdTokenAsync(Req());

        Assert.Equal("https://cdn.sportico.example/my-own.png", existing.AvatarUrl);
    }

    [Fact]
    public async Task ExistingCustomFullName_IsNotOverwritten()
    {
        var existing = GoogleAuthTestData.ExistingUser(fullName: "Tên Do Người Dùng Đặt");
        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { existing });

        await svc.LoginWithIdTokenAsync(Req());

        Assert.Equal("Tên Do Người Dùng Đặt", existing.FullName);
    }

    // 11. An empty avatar IS filled in from Google — backfill, not overwrite.
    [Fact]
    public async Task EmptyAvatar_IsBackfilledFromGoogle()
    {
        var existing = GoogleAuthTestData.ExistingUser(avatarUrl: null);
        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { existing });

        await svc.LoginWithIdTokenAsync(Req());

        Assert.StartsWith("https://lh3.googleusercontent.com/", existing.AvatarUrl);
    }

    // 12. FullName is NOT NULL in the database — a nameless Google profile still has to work.
    [Fact]
    public async Task MissingGoogleName_FallsBackToEmailLocalPart()
    {
        var (svc, users, _, _, _) = GoogleAuthServiceBuilder.Build(
            identity: GoogleAuthTestData.Identity(fullName: null));

        await svc.LoginWithIdTokenAsync(Req());

        Assert.Equal("learner", Assert.Single(users.Users).FullName);
    }

    // 13. Email is normalised the same way the password login normalises it.
    [Fact]
    public async Task MixedCaseGoogleEmail_IsNormalisedToLowercase()
    {
        var (svc, users, _, _, _) = GoogleAuthServiceBuilder.Build(
            identity: GoogleAuthTestData.Identity(email: "  Learner@Gmail.COM  "));

        await svc.LoginWithIdTokenAsync(Req());

        Assert.Equal("learner@gmail.com", Assert.Single(users.Users).Email);
    }

    // 14. Missing client id must be a clean 503, never a 500 or a leaked value.
    [Fact]
    public async Task MissingClientId_ReturnsServiceUnavailable_WithKeyNameOnly()
    {
        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build(clientId: null);

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => svc.LoginWithIdTokenAsync(Req()));

        Assert.Equal(ErrorCodes.GoogleConfigurationMissing, ex.Code);
        Assert.Contains("GOOGLE_CLIENT_ID", ex.Details!);
        Assert.DoesNotContain("secret", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
