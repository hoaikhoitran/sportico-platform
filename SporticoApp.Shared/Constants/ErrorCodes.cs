using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Shared.Constants
{
    public static class ErrorCodes
    {
        // Auth
        public const string InvalidCredentials =
            "AUTH_INVALID_CREDENTIALS";

        public const string InvalidRefreshToken =
            "AUTH_INVALID_REFRESH_TOKEN";

        public const string RefreshTokenExpired =
            "AUTH_REFRESH_TOKEN_EXPIRED";

        public const string AccountInactive =
            "AUTH_ACCOUNT_INACTIVE";

        public const string InvalidVerificationToken =
            "AUTH_INVALID_VERIFICATION_TOKEN";

        // User
        public const string UserNotFound =
            "USER_NOT_FOUND";

        public const string EmailAlreadyExists =
            "USER_EMAIL_ALREADY_EXISTS";

        // Common
        public const string ValidationError =
            "COMMON_VALIDATION_ERROR";

        public const string Forbidden =
            "COMMON_FORBIDDEN";

        public const string InternalServerError =
            "COMMON_INTERNAL_SERVER_ERROR";

        public const string AccountNotActive =
            "COMMON_ACCOUNT_NOT_ACTIVE";

        public const string RoleNotFound =
            "COMMON_ROLE_NOT_FOUND";

    }
}
