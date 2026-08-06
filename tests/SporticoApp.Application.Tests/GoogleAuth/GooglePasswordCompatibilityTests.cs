using SporticoApp.Shared.Helpers;
using Xunit;

namespace SporticoApp.Application.Tests.GoogleAuth;

/// <summary>
/// A Google-only account has PasswordHash == null. Password login must treat that as an ordinary
/// failed login — the bug this guards against is BCrypt throwing on a null hash and turning a
/// routine 401 into a 500.
/// </summary>
public class GooglePasswordCompatibilityTests
{
    // 1. A real password still verifies — the null-hardening did not break normal login.
    [Fact]
    public void ExistingPassword_StillVerifies()
    {
        var hash = PasswordHelper.HashPassword("Correct-Horse-1");

        Assert.True(PasswordHelper.VerifyPassword("Correct-Horse-1", hash));
        Assert.False(PasswordHelper.VerifyPassword("wrong-password", hash));
    }

    // 2. Null hash (Google-only account) → false, and crucially NO exception.
    [Fact]
    public void NullPasswordHash_ReturnsFalse_DoesNotThrow()
    {
        var ex = Record.Exception(() => PasswordHelper.VerifyPassword("any-password", null));

        Assert.Null(ex);
        Assert.False(PasswordHelper.VerifyPassword("any-password", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespaceHash_ReturnsFalse_DoesNotThrow(string hash)
    {
        var ex = Record.Exception(() => PasswordHelper.VerifyPassword("any-password", hash));

        Assert.Null(ex);
        Assert.False(PasswordHelper.VerifyPassword("any-password", hash));
    }

    // 3. A malformed hash must also degrade to "no match" rather than a salt-parsing crash.
    [Fact]
    public void MalformedHash_ReturnsFalse_DoesNotThrow()
    {
        var ex = Record.Exception(() => PasswordHelper.VerifyPassword("any-password", "not-a-bcrypt-hash"));

        Assert.Null(ex);
        Assert.False(PasswordHelper.VerifyPassword("any-password", "not-a-bcrypt-hash"));
    }

    // 4. A null/empty supplied password never matches, even against a valid hash.
    [Fact]
    public void NullOrEmptyPassword_NeverMatches()
    {
        var hash = PasswordHelper.HashPassword("Correct-Horse-1");

        Assert.False(PasswordHelper.VerifyPassword(null, hash));
        Assert.False(PasswordHelper.VerifyPassword("", hash));
    }

    // 5. A password set later (via reset-password) verifies normally — the documented route for a
    //    Google-only user to gain a local password.
    [Fact]
    public void PasswordSetAfterGoogleSignup_VerifiesNormally()
    {
        string? hash = null;                                    // Google-only account
        Assert.False(PasswordHelper.VerifyPassword("New-Password-1", hash));

        hash = PasswordHelper.HashPassword("New-Password-1");    // reset-password sets one
        Assert.True(PasswordHelper.VerifyPassword("New-Password-1", hash));
    }
}
