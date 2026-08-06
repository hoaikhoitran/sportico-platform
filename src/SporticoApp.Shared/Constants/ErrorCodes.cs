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

        public const string InvalidPasswordResetToken =
            "AUTH_INVALID_PASSWORD_RESET_TOKEN";

        public const string PasswordResetTokenExpired =
            "AUTH_PASSWORD_RESET_TOKEN_EXPIRED";

        public const string InvalidCurrentPassword =
            "AUTH_INVALID_CURRENT_PASSWORD";

        public const string EmailAlreadyVerified =
            "AUTH_EMAIL_ALREADY_VERIFIED";

        // User
        public const string UserNotFound =
            "USER_NOT_FOUND";

        public const string EmailAlreadyExists =
            "USER_EMAIL_ALREADY_EXISTS";

        public const string ProfileUpdateFailed =
            "USER_PROFILE_UPDATE_FAILED";

        public const string InvalidImageUrl =
            "USER_INVALID_IMAGE_URL";

        public const string DateOfBirthInvalid =
            "USER_DATE_OF_BIRTH_INVALID";

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
        public const string CoachProfileMediaNotFound =
            "COACH_PROFILE_MEDIA_NOT_FOUND";
        public const string CoachProfileMediaNotOwned =
            "COACH_PROFILE_MEDIA_NOT_OWNED";
        public const string InvalidCoachProfileMediaType =
            "COACH_PROFILE_MEDIA_INVALID_TYPE";
        public const string CoachTeachingLocationNotFound =
            "COACH_TEACHING_LOCATION_NOT_FOUND";
        public const string CoachTeachingLocationNotOwned =
            "COACH_TEACHING_LOCATION_NOT_OWNED";

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
        public const string PayOsPayoutFailed =
            "PAYOS_PAYOUT_FAILED";
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
        public const string TrainingPackageHasNoSchedule =
            "TRAINING_PACKAGE_HAS_NO_SCHEDULE";
        public const string TrainingPackageSessionSlotFull =
            "TRAINING_PACKAGE_SESSION_SLOT_FULL";

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

        public const string InvalidTrainingPlanStatus =
            "INVALID_TRAINING_PLAN_STATUS";

        // Training session
        public const string TrainingSessionNotFound =
            "TRAINING_SESSION_NOT_FOUND";
        public const string TrainingSessionNotOwned =
            "TRAINING_SESSION_NOT_OWNED";
        public const string InvalidTrainingSessionStatus =
            "INVALID_TRAINING_SESSION_STATUS";
        public const string ScheduleConflict =
            "SCHEDULE_CONFLICT";

        /// <summary>Generic optimistic-concurrency clash (slot seat / wallet) — caller should retry.</summary>
        public const string ConcurrencyConflict =
            "CONCURRENCY_CONFLICT";

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

        // Platform settings
        /// <summary>The platform_settings storage is missing/unmigrated — an ops issue, not user error.</summary>
        public const string PlatformSettingsUnavailable =
            "PLATFORM_SETTINGS_UNAVAILABLE";

        // Payment
        public const string InvalidCommissionRate =
            "INVALID_COMMISSION_RATE";
        public const string PaymentAlreadyExists =
            "PAYMENT_ALREADY_EXISTS";
        public const string PaymentNotFound =
            "PAYMENT_NOT_FOUND";

        // Feature flags
        public const string ManualPurchaseDisabled =
            "MANUAL_PURCHASE_DISABLED";

        // Review
        public const string ReviewNotFound =
            "REVIEW_NOT_FOUND";
        public const string ReviewAlreadyExists =
            "REVIEW_ALREADY_EXISTS";
        public const string ReviewNotOwned =
            "REVIEW_NOT_OWNED";
        public const string ReviewNotAllowed =
            "REVIEW_NOT_ALLOWED";
        public const string ReviewEditExpired =
            "REVIEW_EDIT_EXPIRED";
        public const string InvalidRating =
            "INVALID_RATING";

        // Review report / moderation
        public const string ReviewReportNotFound =
            "REVIEW_REPORT_NOT_FOUND";
        public const string ReviewReportNotAllowed =
            "REVIEW_REPORT_NOT_ALLOWED";

        // Advisory chatbot
        public const string AdvisoryConversationNotFound =
            "ADVISORY_CONVERSATION_NOT_FOUND";
        public const string AdvisoryConversationNotOwned =
            "ADVISORY_CONVERSATION_NOT_OWNED";
        public const string AdvisoryReplyFailed =
            "ADVISORY_REPLY_FAILED";

        // Voucher
        public const string VoucherNotFound = "VOUCHER_NOT_FOUND";
        public const string VoucherNotActive = "VOUCHER_NOT_ACTIVE";
        public const string VoucherNotStarted = "VOUCHER_NOT_STARTED";
        public const string VoucherExpired = "VOUCHER_EXPIRED";
        public const string VoucherMinOrderNotMet = "VOUCHER_MIN_ORDER_NOT_MET";
        public const string VoucherUsageLimitReached = "VOUCHER_USAGE_LIMIT_REACHED";
        public const string VoucherLearnerLimitReached = "VOUCHER_LEARNER_LIMIT_REACHED";
        public const string VoucherNotApplicable = "VOUCHER_NOT_APPLICABLE";
        public const string VoucherBudgetExceeded = "VOUCHER_BUDGET_EXCEEDED";
        public const string VoucherAlreadyUsedForBooking = "VOUCHER_ALREADY_USED_FOR_BOOKING";
        public const string VoucherConcurrencyConflict = "VOUCHER_CONCURRENCY_CONFLICT";
        public const string VoucherCampaignNotFound = "VOUCHER_CAMPAIGN_NOT_FOUND";
        public const string VoucherCodeAlreadyExists = "VOUCHER_CODE_ALREADY_EXISTS";
        public const string VoucherCampaignHasRedemptions = "VOUCHER_CAMPAIGN_HAS_REDEMPTIONS";
        public const string VoucherInvalidDateRange = "VOUCHER_INVALID_DATE_RANGE";
        public const string VoucherCampaignAlreadyEnded = "VOUCHER_CAMPAIGN_ALREADY_ENDED";

        // Community
        public const string CommunityPostNotFound = "COMMUNITY_POST_NOT_FOUND";
        public const string CommunityPostNotOwned = "COMMUNITY_POST_NOT_OWNED";
        public const string CommunityPostNotPublished = "COMMUNITY_POST_NOT_PUBLISHED";
        public const string CommunityPostFull = "COMMUNITY_POST_FULL";
        public const string CommunityPostExpired = "COMMUNITY_POST_EXPIRED";
        public const string CommunityPostInvalidStatus = "COMMUNITY_POST_INVALID_STATUS";
        public const string CommunityPostTooManyMedia = "COMMUNITY_POST_TOO_MANY_MEDIA";
        public const string CommunityCommentNotFound = "COMMUNITY_COMMENT_NOT_FOUND";
        public const string CommunityCommentNotOwned = "COMMUNITY_COMMENT_NOT_OWNED";
        public const string CommunityCommentNestingNotAllowed = "COMMUNITY_COMMENT_NESTING_NOT_ALLOWED";
        public const string CommunityCommentsDisabled = "COMMUNITY_COMMENTS_DISABLED";
        public const string CommunityApplicationNotFound = "COMMUNITY_APPLICATION_NOT_FOUND";
        public const string CommunityApplicationAlreadyExists = "COMMUNITY_APPLICATION_ALREADY_EXISTS";
        public const string CommunityApplicationNotAllowed = "COMMUNITY_APPLICATION_NOT_ALLOWED";
        public const string CommunityApplicationNotPending = "COMMUNITY_APPLICATION_NOT_PENDING";
        public const string CommunityConcurrencyConflict = "COMMUNITY_CONCURRENCY_CONFLICT";

        // Report (generic)
        public const string ReportNotFound = "REPORT_NOT_FOUND";
        public const string ReportInvalidTarget = "REPORT_INVALID_TARGET";

        // Chat (user-to-user extension)
        public const string ChatCannotMessageSelf = "CHAT_CANNOT_MESSAGE_SELF";
        public const string ChatTargetUserNotFound = "CHAT_TARGET_USER_NOT_FOUND";
        public const string ChatTargetUserInactive = "CHAT_TARGET_USER_INACTIVE";
        public const string ChatRoomNotPending = "CHAT_ROOM_NOT_PENDING";
        public const string ChatRoomRejected = "CHAT_ROOM_REJECTED";
        public const string ChatUserBlocked = "CHAT_USER_BLOCKED";
        public const string ChatEmptyMessage = "CHAT_EMPTY_MESSAGE";
        public const string ChatTooManyAttachments = "CHAT_TOO_MANY_ATTACHMENTS";

        // User block
        public const string UserBlockCannotBlockSelf = "USER_BLOCK_CANNOT_BLOCK_SELF";
        public const string UserBlockAlreadyBlocked = "USER_BLOCK_ALREADY_BLOCKED";
        public const string UserBlockNotFound = "USER_BLOCK_NOT_FOUND";

        // Google authentication
        public const string GoogleInvalidToken = "AUTH_GOOGLE_INVALID_TOKEN";
        public const string GoogleEmailNotVerified = "AUTH_GOOGLE_EMAIL_NOT_VERIFIED";
        public const string GoogleAccountConflict = "AUTH_GOOGLE_ACCOUNT_CONFLICT";
        public const string GoogleLoginFailed = "AUTH_GOOGLE_LOGIN_FAILED";
        public const string GoogleConfigurationMissing = "AUTH_GOOGLE_CONFIGURATION_MISSING";
        public const string GoogleExternalPrincipalInvalid = "AUTH_GOOGLE_EXTERNAL_PRINCIPAL_INVALID";
        public const string GoogleExchangeCodeInvalid = "AUTH_GOOGLE_EXCHANGE_CODE_INVALID";
        public const string GoogleExchangeCodeExpired = "AUTH_GOOGLE_EXCHANGE_CODE_EXPIRED";
        public const string GoogleExchangeCodeAlreadyUsed = "AUTH_GOOGLE_EXCHANGE_CODE_ALREADY_USED";

        /// <summary>A Google-only account has no local password to verify or change.</summary>
        public const string PasswordNotSet = "AUTH_PASSWORD_NOT_SET";
    }
}
