using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Infrastructure.Persistence;
using SporticoApp.Infrastructure.Persistence.Configurations;
using SporticoApp.Infrastructure.Persistence.Repositories;
using SporticoApp.Infrastructure.Services;
using SporticoApp.Infrastructure.Services.Advisory;
using SporticoApp.Infrastructure.Services.Payments;

namespace SporticoApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureDI(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("Default")));

            services.Configure<EmailSettings>(options =>
                configuration.GetSection("EmailSettings").Bind(options));

            services.Configure<PayOsSettings>(options =>
                configuration.GetSection("PayOs").Bind(options));

            // Gemini advisory chatbot settings (API key comes from configuration / user secrets).
            services.Configure<GeminiSettings>(options =>
                configuration.GetSection("Gemini").Bind(options));

            // Dedicated PayOS Payout (Chi) channel credentials — kept separate from the inbound
            // PayOs payment-link credentials. Real values come from PayOsPayout__* env vars.
            services.Configure<PayOsPayoutSettings>(options =>
                configuration.GetSection("PayOsPayout").Bind(options));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<ICoachRepository, CoachRepository>();
            services.AddScoped<ICoachProfileMediaRepository, CoachProfileMediaRepository>();
            services.AddScoped<ICoachTeachingLocationRepository, CoachTeachingLocationRepository>();
            services.AddScoped<ISportRepository, SportRepository>();
            services.AddScoped<IPackageRepository, PackageRepository>();
            services.AddScoped<ICoachPackageRepository, CoachPackageRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ITrainingPackageRepository, TrainingPackageRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<ITrainingSessionRepository, TrainingSessionRepository>();
            services.AddScoped<ILearnerAssessmentRepository, LearnerAssessmentRepository>();
            services.AddScoped<ITrainingPlanRepository, TrainingPlanRepository>();
            services.AddScoped<IProgressCheckInRepository, ProgressCheckInRepository>();
            services.AddScoped<ICoachPayoutAccountRepository, CoachPayoutAccountRepository>();
            services.AddScoped<ICoachWalletRepository, CoachWalletRepository>();
            services.AddScoped<IWithdrawalRequestRepository, WithdrawalRequestRepository>();
            services.AddScoped<IChatRepository, ChatRepository>();
            services.AddScoped<IAdvisoryConversationRepository, AdvisoryConversationRepository>();
            services.AddScoped<ICoachAvailabilityRepository, CoachAvailabilityRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IPublicCoachRepository, PublicCoachRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IReviewReportRepository, ReviewReportRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IPlatformSettingRepository, PlatformSettingRepository>();
            services.AddScoped<IAdminPaymentRepository, AdminPaymentRepository>();
            services.AddScoped<IVisitorTrackingRepository, VisitorTrackingRepository>();
            // Singleton: the Channel<T> buffer must be the SAME instance shared by every request
            // (producer) and the one background consumer — never scoped/transient.
            services.AddSingleton<IVisitorTrackingQueue, VisitorTrackingQueue>();
            services.AddScoped<IVisitorAnalyticsRepository, VisitorAnalyticsRepository>();
            services.AddSingleton<IUserAgentParser, UserAgentParser>();
            services.AddScoped<IVoucherCampaignRepository, VoucherCampaignRepository>();
            services.AddScoped<IVoucherRedemptionRepository, VoucherRedemptionRepository>();
            services.AddScoped<IUserBlockRepository, UserBlockRepository>();
            services.AddScoped<ICommunityPostRepository, CommunityPostRepository>();
            services.AddScoped<ICommunityCommentRepository, CommunityCommentRepository>();
            services.AddScoped<ICommunityPostReactionRepository, CommunityPostReactionRepository>();
            services.AddScoped<ICommunityPostApplicationRepository, CommunityPostApplicationRepository>();
            services.AddScoped<ICommunityReportRepository, CommunityReportRepository>();

            services.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();
            services.AddScoped<IAuthExchangeCodeRepository, AuthExchangeCodeRepository>();

            services.AddScoped<IJwtService, JwtService>();
            // Verifies Google ID tokens (signature/issuer/audience/expiry) via Google.Apis.Auth.
            services.AddScoped<IGoogleIdentityProvider, GoogleIdentityProvider>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
            services.AddSingleton<ISlugGenerator, SlugGenerator>();

            services.AddHttpClient<IPayOsService, PayOsService>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<PayOsSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
            });

            // Gemini advisory chatbot HTTP client (Google Generative Language API).
            services.AddHttpClient<IGeminiAdvisoryService, GeminiAdvisoryService>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<GeminiSettings>>().Value;
                var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
                    ? "https://generativelanguage.googleapis.com"
                    : settings.BaseUrl;
                client.BaseAddress = new Uri(baseUrl);
            });

            // Payout (Chi) HTTP client uses the dedicated payout BaseUrl, falling back to the
            // inbound PayOs BaseUrl, then the PayOS default, for backward compatibility.
            services.AddHttpClient<IPayOsPayoutService, PayOsPayoutService>((sp, client) =>
            {
                var payoutBaseUrl = configuration["PayOsPayout:BaseUrl"];
                if (string.IsNullOrWhiteSpace(payoutBaseUrl))
                {
                    payoutBaseUrl = configuration["PayOs:BaseUrl"];
                }
                if (string.IsNullOrWhiteSpace(payoutBaseUrl))
                {
                    payoutBaseUrl = "https://api-merchant.payos.vn";
                }

                client.BaseAddress = new Uri(payoutBaseUrl);
            });

            // Bind PayoutOptions so the Application layer can read AutoPayoutEnabled and
            // PayoutCategory without depending on Infrastructure. Prefer the dedicated PayOsPayout
            // section, falling back to the legacy PayOs section for backward compatibility.
            services.Configure<SporticoApp.Application.Options.PayoutOptions>(options =>
            {
                var payout = configuration.GetSection("PayOsPayout");
                var payin = configuration.GetSection("PayOs");

                options.AutoPayoutEnabled =
                    payout.GetValue<bool?>("AutoPayoutEnabled")
                    ?? payin.GetValue<bool>("AutoPayoutEnabled");

                // Category is intentionally nullable — omit from the PayOS body entirely when not set.
                // Never fall back to "salary"; PayOS rejects categories the merchant account did not configure.
                var category = payout.GetValue<string?>("PayoutCategory");
                if (string.IsNullOrWhiteSpace(category))
                {
                    category = payin.GetValue<string?>("PayoutCategory");
                }
                options.PayoutCategory = string.IsNullOrWhiteSpace(category) ? null : category;
            });

            // Backend feature flags (e.g. dev/test manual purchase). Defaults to all-off
            // when the "Features" section is absent.
            services.Configure<SporticoApp.Application.Options.FeatureOptions>(
                configuration.GetSection("Features"));

            services.Configure<SporticoApp.Application.Options.AnalyticsOptions>(
                configuration.GetSection(SporticoApp.Application.Options.AnalyticsOptions.SectionName));

            return services;
        }
    }
}
