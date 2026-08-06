using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Coaches;

namespace SporticoApp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationDI(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<ICoachService, CoachService>();
            services.AddScoped<ICoachProfileMediaService, CoachProfileMediaService>();
            services.AddScoped<ICoachTeachingLocationService, CoachTeachingLocationService>();
            services.AddScoped<ICoachPackageService, CoachPackageService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IAdminPostService, AdminPostService>();
            services.AddScoped<ISportService, SportService>();
            services.AddScoped<ITrainingPackageService, TrainingPackageService>();
            services.AddScoped<IAdminTrainingPackageService, AdminTrainingPackageService>();
            services.AddScoped<IPublicTrainingPackageService, PublicTrainingPackageService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IBookingSessionUsageService, BookingSessionUsageService>();
            services.AddScoped<ITrainingSessionService, TrainingSessionService>();
            services.AddScoped<ILearnerAssessmentService, LearnerAssessmentService>();
            services.AddScoped<ITrainingPlanService, TrainingPlanService>();
            services.AddScoped<IProgressCheckInService, ProgressCheckInService>();
            services.AddScoped<ICoachPayoutAccountService, CoachPayoutAccountService>();
            services.AddScoped<ICoachWalletService, CoachWalletService>();
            services.AddScoped<IWithdrawalService, WithdrawalService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IAdvisoryService, AdvisoryService>();
            services.AddScoped<ICoachAvailabilityService, CoachAvailabilityService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IPublicCoachService, PublicCoachService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IReviewReportService, ReviewReportService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IAdminUserService, AdminUserService>();
            services.AddScoped<IUserPublicService, UserPublicService>();
            services.AddScoped<IPlatformSettingService, PlatformSettingService>();
            services.AddScoped<IAdminPaymentService, AdminPaymentService>();
            services.AddScoped<IVisitorTrackingService, VisitorTrackingService>();
            services.AddScoped<IVisitorAnalyticsService, VisitorAnalyticsService>();
            services.AddScoped<IVoucherService, VoucherService>();
            services.AddScoped<IUserBlockService, UserBlockService>();
            services.AddScoped<ICommunityPostService, CommunityPostService>();
            services.AddScoped<ICommunityCommentService, CommunityCommentService>();
            services.AddScoped<ICommunityReportService, CommunityReportService>();
            services.AddScoped<IAdminCommunityService, AdminCommunityService>();
            // Single source of truth for Sportico session tokens — used by password login,
            // refresh-token, and both Google flows.
            services.AddScoped<ITokenIssuer, TokenIssuer>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();

            services.AddValidatorsFromAssemblyContaining
                <RegisterCoachRequestValidator>();

            return services;
        }
    }
}
