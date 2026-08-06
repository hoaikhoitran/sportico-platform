using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Controllers;
using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.Validators.Auth;
using SporticoApp.Shared.Enums;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.GoogleAuth;

/// <summary>
/// The HTTP contract itself: request validation, routes/verbs (which is what Swagger publishes),
/// anonymous access, and the 503 mapping for missing configuration.
/// </summary>
public class GoogleAuthContractTests
{
    // ── Request validation ──────────────────────────────────────────────────

    [Fact]
    public void IdTokenRequest_EmptyToken_IsInvalid()
    {
        var result = new GoogleIdTokenLoginRequestValidator()
            .Validate(new GoogleIdTokenLoginRequest { IdToken = "" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void IdTokenRequest_OversizedToken_IsInvalid()
    {
        var result = new GoogleIdTokenLoginRequestValidator()
            .Validate(new GoogleIdTokenLoginRequest { IdToken = new string('a', 8193) });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void IdTokenRequest_NormalToken_IsValid()
    {
        var result = new GoogleIdTokenLoginRequestValidator()
            .Validate(new GoogleIdTokenLoginRequest { IdToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjEifQ.e30.sig" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ExchangeRequest_EmptyCode_IsInvalid(string? code)
    {
        var result = new GoogleExchangeCodeRequestValidator()
            .Validate(new GoogleExchangeCodeRequest { Code = code! });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ExchangeRequest_OversizedCode_IsInvalid()
    {
        var result = new GoogleExchangeCodeRequestValidator()
            .Validate(new GoogleExchangeCodeRequest { Code = new string('c', 257) });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ExchangeRequest_NormalCode_IsValid()
    {
        var result = new GoogleExchangeCodeRequestValidator()
            .Validate(new GoogleExchangeCodeRequest { Code = new string('c', 43) });

        Assert.True(result.IsValid);
    }

    // ── Routes / verbs (what Swagger publishes) ─────────────────────────────

    [Fact]
    public void Controller_IsRoutedAtApiAuthGoogle_AndAnonymous()
    {
        var type = typeof(GoogleAuthController);

        Assert.Equal("api/auth/google", type.GetCustomAttribute<RouteAttribute>()!.Template);
        Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
        Assert.NotNull(type.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(type.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void PostApiAuthGoogle_Exists()
    {
        var method = typeof(GoogleAuthController).GetMethod("LoginWithIdToken");

        var post = method!.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(post);
        Assert.True(string.IsNullOrEmpty(post!.Template));   // POST /api/auth/google
    }

    [Fact]
    public void GetApiAuthGoogle_Exists()
    {
        var method = typeof(GoogleAuthController).GetMethod("StartRedirect");

        var get = method!.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(get);
        Assert.True(string.IsNullOrEmpty(get!.Template));    // GET /api/auth/google
    }

    [Fact]
    public void PostApiAuthGoogleExchange_Exists()
    {
        var method = typeof(GoogleAuthController).GetMethod("Exchange");

        Assert.Equal("exchange", method!.GetCustomAttribute<HttpPostAttribute>()!.Template);
    }

    [Fact]
    public void GetApiAuthGoogleComplete_Exists()
    {
        var method = typeof(GoogleAuthController).GetMethod("Complete");

        Assert.Equal("complete", method!.GetCustomAttribute<HttpGetAttribute>()!.Template);
    }

    [Fact]
    public void IdTokenAndExchange_DeclareResultLoginResponse()
    {
        foreach (var name in new[] { "LoginWithIdToken", "Exchange" })
        {
            var produces = typeof(GoogleAuthController).GetMethod(name)!
                .GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Single(a => a.StatusCode == 200);

            Assert.Equal(
                typeof(SporticoApp.Shared.Responses.Result<LoginResponse>),
                produces.Type);
        }
    }

    // ── Error mapping ───────────────────────────────────────────────────────

    [Fact]
    public void ServiceUnavailableException_MapsToServiceUnavailableErrorType()
    {
        var ex = new ServiceUnavailableException("AUTH_GOOGLE_CONFIGURATION_MISSING", "not configured");

        Assert.Equal(ErrorType.ServiceUnavailable, ex.Type);
    }

    /// <summary>
    /// The existing ErrorType members must keep their numeric values, otherwise previously
    /// serialized values would silently change meaning.
    /// </summary>
    [Fact]
    public void ExistingErrorTypeValues_AreUnchanged()
    {
        Assert.Equal(0, (int)ErrorType.Validation);
        Assert.Equal(1, (int)ErrorType.NotFound);
        Assert.Equal(2, (int)ErrorType.Unauthorized);
        Assert.Equal(3, (int)ErrorType.Forbidden);
        Assert.Equal(4, (int)ErrorType.Conflict);
        Assert.Equal(5, (int)ErrorType.Failure);
        Assert.Equal(6, (int)ErrorType.ServiceUnavailable);   // appended last
    }
}
