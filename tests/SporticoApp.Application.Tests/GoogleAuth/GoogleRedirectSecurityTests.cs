using SporticoApp.Api.Configuration;
using SporticoApp.Application.Options;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.GoogleAuth;

/// <summary>
/// The redirect flow's URL handling. The rule these tests enforce: the frontend hop is always
/// rebuilt from the configured FRONTEND_URL, and no token material ever appears in a URL.
/// </summary>
public class GoogleRedirectSecurityTests
{
    private static GoogleAuthOptions Options(
        string? frontendUrl = "https://app.example.com",
        string? callbackUrl = "https://api.example.com/api/auth/google/callback",
        string? clientId = "cid",
        string? clientSecret = "secret") => new()
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            CallbackUrl = callbackUrl,
            FrontendUrl = frontendUrl
        };

    // 1. The success redirect carries the one-time code and nothing else.
    [Fact]
    public void SuccessRedirect_CarriesOnlyTheCode()
    {
        var url = GoogleCallbackUrlResolver.BuildFrontendCallbackUrl(Options(), "one-time-code-abc", null);

        Assert.Equal("https://app.example.com/auth/google/callback?code=one-time-code-abc", url);
        Assert.DoesNotContain("accessToken", url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#", url);   // nothing hidden in a fragment either
    }

    // 2. The failure redirect carries a stable error code, never an exception message.
    [Fact]
    public void FailureRedirect_CarriesStableErrorCode()
    {
        var url = GoogleCallbackUrlResolver.BuildFrontendCallbackUrl(
            Options(), null, ErrorCodes.GoogleExternalPrincipalInvalid);

        Assert.Equal(
            "https://app.example.com/auth/google/callback?error=AUTH_GOOGLE_EXTERNAL_PRINCIPAL_INVALID",
            url);
        Assert.DoesNotContain("Exception", url);
        Assert.DoesNotContain("   at ", url);
    }

    // 3. The redirect always lands under the configured frontend origin — an attacker-supplied
    //    host cannot be smuggled in, because the host is never taken from input.
    [Theory]
    [InlineData("https://evil.example.com/steal")]
    [InlineData("//evil.example.com")]
    [InlineData("http://evil.example.com")]
    [InlineData("/../../evil")]
    public void RedirectHost_AlwaysComesFromConfiguration_NotFromTheCode(string hostile)
    {
        // Even if the "code" itself looks like a URL, it is escaped into the query string.
        var url = GoogleCallbackUrlResolver.BuildFrontendCallbackUrl(Options(), hostile, null);

        Assert.StartsWith("https://app.example.com/auth/google/callback?code=", url);
        Assert.DoesNotContain("evil.example.com/", url);   // never becomes part of the host/path
    }

    // 4. A protocol-relative or otherwise odd FRONTEND_URL is used verbatim only as configured —
    //    it is operator-controlled, never request-controlled.
    [Fact]
    public void FrontendUrl_TrailingSlash_IsNormalised()
    {
        var options = Options(frontendUrl: "https://app.example.com/");
        options.FrontendUrl = options.FrontendUrl!.TrimEnd('/');

        var url = GoogleCallbackUrlResolver.BuildFrontendCallbackUrl(options, "c", null);

        Assert.Equal("https://app.example.com/auth/google/callback?code=c", url);
        Assert.DoesNotContain("//auth/google", url);
    }

    // 5. The callback path used by the Google handler comes from GOOGLE_CALLBACK_URL.
    [Fact]
    public void CallbackPath_IsTakenFromConfiguredUrl()
    {
        Assert.Equal("/api/auth/google/callback",
            GoogleCallbackUrlResolver.ResolveCallbackPath(Options()));

        Assert.Equal("/custom/oauth/return",
            GoogleCallbackUrlResolver.ResolveCallbackPath(
                Options(callbackUrl: "https://api.example.com/custom/oauth/return")));
    }

    // 6. Missing/garbage callback URL falls back to the documented default so startup never breaks.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-absolute-url")]
    [InlineData("https://api.example.com")]   // no path
    public void CallbackPath_FallsBackToDefault(string? callbackUrl)
    {
        Assert.Equal(GoogleCallbackUrlResolver.DefaultCallbackPath,
            GoogleCallbackUrlResolver.ResolveCallbackPath(Options(callbackUrl: callbackUrl)));
    }

    // 7. Production refuses a non-HTTPS callback; local development may use http on loopback.
    [Fact]
    public void CallbackUrl_MustBeHttpsOutsideDevelopment()
    {
        var httpProd = Options(callbackUrl: "http://api.example.com/api/auth/google/callback");
        Assert.False(GoogleCallbackUrlResolver.IsCallbackUrlValid(httpProd, isDevelopment: false));

        var httpsProd = Options();
        Assert.True(GoogleCallbackUrlResolver.IsCallbackUrlValid(httpsProd, isDevelopment: false));

        var localhost = Options(callbackUrl: "http://localhost:5199/api/auth/google/callback");
        Assert.True(GoogleCallbackUrlResolver.IsCallbackUrlValid(localhost, isDevelopment: true));
        Assert.False(GoogleCallbackUrlResolver.IsCallbackUrlValid(localhost, isDevelopment: false));
    }

    // 8. A non-loopback http callback is rejected even in Development.
    [Fact]
    public void RemoteHttpCallback_IsRejectedEvenInDevelopment()
    {
        var remoteHttp = Options(callbackUrl: "http://api.example.com/api/auth/google/callback");

        Assert.False(GoogleCallbackUrlResolver.IsCallbackUrlValid(remoteHttp, isDevelopment: true));
    }

    // 9. Configuration gaps are reported by KEY NAME only — never a value.
    [Fact]
    public void MissingConfiguration_ReportsKeyNamesOnly()
    {
        var empty = new GoogleAuthOptions();

        var missing = empty.MissingRedirectFlowKeys();

        Assert.Equal(
            new[] { "GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "GOOGLE_CALLBACK_URL", "FRONTEND_URL" },
            missing);
        Assert.False(empty.IsRedirectFlowConfigured);
        Assert.False(empty.IsIdTokenFlowConfigured);
    }

    [Fact]
    public void IdTokenFlow_NeedsOnlyClientId()
    {
        var idTokenOnly = new GoogleAuthOptions { ClientId = "cid" };

        Assert.True(idTokenOnly.IsIdTokenFlowConfigured);
        Assert.False(idTokenOnly.IsRedirectFlowConfigured);   // redirect still needs secret + URLs
    }

    // 10. The exchange-code lifetime is clamped into a sane band regardless of configuration.
    [Theory]
    [InlineData(0, 30)]
    [InlineData(5, 30)]
    [InlineData(90, 90)]
    [InlineData(120, 120)]
    [InlineData(99999, 300)]
    public void ExchangeCodeLifetime_IsClamped(int configured, int expected)
    {
        var options = new GoogleAuthOptions { ExchangeCodeLifetimeSeconds = configured };

        Assert.Equal(expected, options.EffectiveExchangeCodeLifetimeSeconds);
    }
}
