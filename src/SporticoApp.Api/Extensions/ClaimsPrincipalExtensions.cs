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
}