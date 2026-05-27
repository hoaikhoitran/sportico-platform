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

        // Training package
        public const string TrainingPackageNotFound =
            "TRAINING_PACKAGE_NOT_FOUND";
        public const string TrainingPackageNotPublished =
            "TRAINING_PACKAGE_NOT_PUBLISHED";
        public const string TrainingPackageNotOwned =
            "TRAINING_PACKAGE_NOT_OWNED";
        public const string InvalidTrainingPackageStatus =
            "INVALID_TRAINING_PACKAGE_STATUS";

        // Booking
        public const string BookingNotFound =
            "BOOKING_NOT_FOUND";
        public const string BookingNotOwned =
            "BOOKING_NOT_OWNED";
        public const string BookingNotActive =
            "BOOKING_NOT_ACTIVE";
        public const string BookingAlreadyCompleted =
            "BOOKING_ALREADY_COMPLETED";
        public const string SessionLimitExceeded =
            "SESSION_LIMIT_EXCEEDED";

        // Training session
        public const string TrainingSessionNotFound =
            "TRAINING_SESSION_NOT_FOUND";
        public const string TrainingSessionNotOwned =
            "TRAINING_SESSION_NOT_OWNED";
        public const string InvalidTrainingSessionStatus =
            "INVALID_TRAINING_SESSION_STATUS";
        public const string ScheduleConflict =
            "SCHEDULE_CONFLICT";

        // Personalization
        public const string LearnerAssessmentNotFound =
            "LEARNER_ASSESSMENT_NOT_FOUND";
        public const string TrainingPlanNotFound =
            "TRAINING_PLAN_NOT_FOUND";
        public const string TrainingPlanNotOwned =
            "TRAINING_PLAN_NOT_OWNED";
        public const string ProgressCheckInNotFound =
            "PROGRESS_CHECKIN_NOT_FOUND";

        // Wallet and payout
        public const string CoachWalletNotFound =
            "COACH_WALLET_NOT_FOUND";
        public const string CoachPayoutAccountNotFound =
            "COACH_PAYOUT_ACCOUNT_NOT_FOUND";
        public const string WithdrawalRequestNotFound =
            "WITHDRAWAL_REQUEST_NOT_FOUND";
        public const string InsufficientWalletBalance =
            "INSUFFICIENT_WALLET_BALANCE";
        public const string PayoutAccountRequired =
            "PAYOUT_ACCOUNT_REQUIRED";

        // Chat
        public const string ChatNotAllowed =
            "CHAT_NOT_ALLOWED";

        public const string NotificationNotFound =
            "NOTIFICATION_NOT_FOUND";

        // Payment
        public const string InvalidCommissionRate =
            "INVALID_COMMISSION_RATE";
        public const string PaymentAlreadyExists =
            "PAYMENT_ALREADY_EXISTS";
        public const string PaymentNotFound =
            "PAYMENT_NOT_FOUND";
    }
}
