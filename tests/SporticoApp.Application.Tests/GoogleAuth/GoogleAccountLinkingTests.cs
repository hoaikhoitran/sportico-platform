using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.GoogleAuth;

/// <summary>
/// The linking rules that keep one human = one Sportico account: the Google "sub" is the identity,
/// email is only a bridge to an existing account, and neither side may fan out.
/// </summary>
public class GoogleAccountLinkingTests
{
    private static GoogleIdTokenLoginRequest Req() => new() { IdToken = "any-google-id-token" };

    private static UserExternalLogin Link(Guid userId, string subject = GoogleAuthTestData.Subject) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Provider = ExternalAuthProviders.Google,
        ProviderSubject = subject,
        ProviderEmail = GoogleAuthTestData.Email,
        CreatedAt = DateTime.UtcNow.AddDays(-1),
        LastLoginAt = DateTime.UtcNow.AddDays(-1)
    };

    // 1. Signing in twice with the same Google account returns the SAME Sportico user.
    [Fact]
    public async Task SameGoogleSubject_ReturnsSameUser_AndCreatesNoSecondLink()
    {
        var (svc, users, links, _, _) = GoogleAuthServiceBuilder.Build();

        await svc.LoginWithIdTokenAsync(Req());
        var firstUserId = Assert.Single(users.Users).Id;

        await svc.LoginWithIdTokenAsync(Req());

        Assert.Single(users.Users);
        Assert.Equal(firstUserId, users.Users[0].Id);
        Assert.Single(links.Links);
    }

    // 2. A repeat sign-in refreshes LastLoginAt.
    [Fact]
    public async Task RepeatLogin_UpdatesLastLoginAt()
    {
        var existing = GoogleAuthTestData.ExistingUser();
        var link = Link(existing.Id);
        var before = link.LastLoginAt!.Value;

        var (svc, _, _, _, _) = GoogleAuthServiceBuilder.Build(
            seedUsers: new[] { existing }, seedLinks: new[] { link });

        await svc.LoginWithIdTokenAsync(Req());

        Assert.True(link.LastLoginAt > before);
    }

    // 3. Same email must never produce a second account.
    [Fact]
    public async Task SameEmail_DoesNotCreateDuplicateUser()
    {
        var existing = GoogleAuthTestData.ExistingUser();
        var (svc, users, _, _, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { existing });

        await svc.LoginWithIdTokenAsync(Req());

        Assert.Single(users.Users);
        Assert.Equal(existing.Id, users.Users[0].Id);
    }

    // 4. One Sportico account cannot be claimed by a second, different Google account.
    [Fact]
    public async Task OneUser_CannotLinkTwoDifferentGoogleSubjects()
    {
        var existing = GoogleAuthTestData.ExistingUser();
        var alreadyLinked = Link(existing.Id, subject: "google-sub-ORIGINAL");

        var (svc, _, links, _, _) = GoogleAuthServiceBuilder.Build(
            identity: GoogleAuthTestData.Identity(subject: "google-sub-DIFFERENT"),
            seedUsers: new[] { existing },
            seedLinks: new[] { alreadyLinked });

        var ex = await Assert.ThrowsAsync<ConflictException>(() => svc.LoginWithIdTokenAsync(Req()));

        Assert.Equal(ErrorCodes.GoogleAccountConflict, ex.Code);
        Assert.Single(links.Links);   // the original link is untouched
    }

    // 5. One Google account resolves to exactly one Sportico user, even when another user shares
    //    the email — the subject lookup wins and no second link is created.
    [Fact]
    public async Task OneGoogleSubject_ResolvesToItsLinkedUser_NotTheSameEmailUser()
    {
        var linkedUser = GoogleAuthTestData.ExistingUser(email: "linked@gmail.com");
        var sameEmailUser = GoogleAuthTestData.ExistingUser(email: GoogleAuthTestData.Email);
        var link = Link(linkedUser.Id);

        var (svc, users, links, _, _) = GoogleAuthServiceBuilder.Build(
            seedUsers: new[] { linkedUser, sameEmailUser },
            seedLinks: new[] { link });

        var result = await svc.LoginWithIdTokenAsync(Req());

        Assert.True(result.IsSuccess);
        Assert.Single(links.Links);
        Assert.Equal(2, users.Users.Count);                          // nothing created
        Assert.Equal("access-token-for-" + linkedUser.Id, result.Data!.AccessToken);
    }

    // 6. Brand-new Google users get exactly one role: learner. Never coach, never admin.
    [Fact]
    public async Task NewGoogleAccount_ReceivesLearnerRoleOnly()
    {
        var (svc, _, _, userRoles, _) = GoogleAuthServiceBuilder.Build();

        await svc.LoginWithIdTokenAsync(Req());

        var assigned = Assert.Single(userRoles.Added);
        Assert.Equal(1, assigned.RoleId);   // the fake learner role
    }

    // 7. Linking Google to an existing coach/admin must not disturb their roles.
    [Fact]
    public async Task ExistingRoles_ArePreservedWhenLinking()
    {
        var coach = GoogleAuthTestData.ExistingUser();
        coach.UserRoles.Add(new UserRole { UserId = coach.Id, RoleId = 2 });   // coach
        coach.UserRoles.Add(new UserRole { UserId = coach.Id, RoleId = 3 });   // admin

        var (svc, _, _, userRoles, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { coach });

        await svc.LoginWithIdTokenAsync(Req());

        Assert.Equal(2, coach.UserRoles.Count);
        Assert.Empty(userRoles.Added);   // no learner role bolted on
    }

    // 8. A concurrent first login losing the unique-constraint race must reuse the winner's
    //    account rather than 500 or duplicate.
    [Fact]
    public async Task ConcurrentFirstLogin_LosesRace_ReusesWinnersAccount()
    {
        var (svc, users, links, _, _) = GoogleAuthServiceBuilder.Build();

        // Simulate: while we were inserting, another request already created user + link.
        var winner = GoogleAuthTestData.ExistingUser(email: GoogleAuthTestData.Email);
        users.OnSaveNewUser = _ =>
        {
            users.Users.Add(winner);
            links.Links.Add(Link(winner.Id));
            return new InvalidOperationException(
                "23505: duplicate key value violates unique constraint \"users_email_key\"");
        };

        var result = await svc.LoginWithIdTokenAsync(Req());

        Assert.True(result.IsSuccess);
        Assert.Single(users.Users);
        Assert.Single(links.Links);
        Assert.Equal("access-token-for-" + winner.Id, result.Data!.AccessToken);
    }

    // 9. Linking an inactive account clears the stale verification token.
    [Fact]
    public async Task LinkingInactiveAccount_ClearsVerificationToken()
    {
        var inactive = GoogleAuthTestData.ExistingUser(status: "inactive");
        inactive.EmailVerificationToken = "still-pending";

        var (svc, _, links, _, _) = GoogleAuthServiceBuilder.Build(seedUsers: new[] { inactive });

        await svc.LoginWithIdTokenAsync(Req());

        Assert.Null(inactive.EmailVerificationToken);
        Assert.Equal("active", inactive.Status);
        Assert.Single(links.Links);
    }

    // 10. The link records the provider email for audit, and uses the normalised form.
    [Fact]
    public async Task Link_RecordsNormalisedProviderEmail()
    {
        var (svc, _, links, _, _) = GoogleAuthServiceBuilder.Build(
            identity: GoogleAuthTestData.Identity(email: "Learner@GMAIL.com"));

        await svc.LoginWithIdTokenAsync(Req());

        Assert.Equal("learner@gmail.com", Assert.Single(links.Links).ProviderEmail);
    }
}
