using System.Security.Claims;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdValue) ||
            !Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedException(
                ErrorCodes.InvalidCredentials,
                "Invalid access token");
        }

        return userId;
    }

    /// <summary>
    /// Returns the user id when the request is authenticated, otherwise null.
    /// Used by endpoints that are public but enrich the response for signed-in users
    /// (e.g. the per-review CanEdit flag on public review listings).
    /// </summary>
    public static Guid? GetUserIdOrNull(this ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}