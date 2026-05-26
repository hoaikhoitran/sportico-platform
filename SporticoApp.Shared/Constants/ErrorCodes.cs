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
        // Coach
        public const string CoachProfileAlreadyExists =
            "COACH_PROFILE_ALREADY_EXISTS";
        public const string CoachProfileRequired =
            "COACH_PROFILE_REQUIRED";
        public const string CoachPackageStillActive =
            "COACH_PACKAGE_STILL_ACTIVE";
        public const string CoachPackagePendingPayment =
            "COACH_PACKAGE_PENDING_PAYMENT";
        public const string CoachProfileNotFound =
            "COACH_PROFILE_NOT_FOUND";
        public const string CoachPackageNotFound =
            "COACH_PACKAGE_NOT_FOUND";

        // Sport
        public const string InvalidSport =
            "SPORT_INVALID";

        public const string SportNameAlreadyExists =
            "SPORT_NAME_ALREADY_EXISTS";

        public const string SportSlugAlreadyExists =
            "SPORT_SLUG_ALREADY_EXISTS";

        public const string SportNotFound =
            "SPORT_NOT_FOUND";

        public const string InvalidSportSlug =
            "SPORT_INVALID_SLUG";
        //PayOS
        public const string PayOsCreatePaymentFailed =
            "PAYOS_CREATE_PAYMENT_FAILED";
        //Package
        public const string PackageNotFound =
            "PACKAGE_NOT_FOUND";
        public const string PackageNameAlreadyExists =
            "PACKAGE_NAME_ALREADY_EXISTS";
        public const string PackageInactive =
            "PACKAGE_INACTIVE";

        // Post
        public const string PostNotFound =
            "POST_NOT_FOUND";
        public const string PostNotOwned =
            "POST_NOT_OWNED";
        public const string ActivePackageRequired =
            "ACTIVE_PACKAGE_REQUIRED";
        public const string PostQuotaExceeded =
            "POST_QUOTA_EXCEEDED";
        public const string InvalidPostStatus =
            "INVALID_POST_STATUS";
    }
}
