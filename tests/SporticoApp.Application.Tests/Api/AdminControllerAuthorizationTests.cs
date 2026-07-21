using Microsoft.AspNetCore.Authorization;
using SporticoApp.Api.Controllers;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Api;

/// <summary>
/// Guards the admin-only authorization declarations against accidental removal. These are
/// declarative attributes, not runtime logic, so this is verified via reflection (matching how the
/// live curl-based checks in the audit confirmed the same behavior at the HTTP layer: 401 without a
/// token, 200 with an admin token).
/// </summary>
public class AdminControllerAuthorizationTests
{
    [Fact]
    public void AdminPaymentsController_RequiresAdminRole()
    {
        var attribute = typeof(AdminPaymentsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal(RoleConstants.Admin, attribute!.Roles);
    }

    [Fact]
    public void AdminVisitorAnalyticsController_RequiresAdminRole()
    {
        var attribute = typeof(AdminVisitorAnalyticsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal(RoleConstants.Admin, attribute!.Roles);
    }

    // The frontend page-view ingestion endpoint MUST remain public (anonymous visitors submit
    // page views) — this guards against someone accidentally locking it behind auth.
    [Fact]
    public void AnalyticsController_DoesNotRequireAuthorization()
    {
        var attribute = typeof(AnalyticsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false);

        Assert.Empty(attribute);
    }
}
