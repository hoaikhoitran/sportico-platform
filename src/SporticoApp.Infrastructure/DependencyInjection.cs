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
            services.AddScoped<ICoachAvailabilityRepository, CoachAvailabilityRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IPublicCoachRepository, PublicCoachRepository>();

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
            services.AddSingleton<ISlugGenerator, SlugGenerator>();

            services.AddHttpClient<IPayOsService, PayOsService>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<PayOsSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
            });

            services.AddHttpClient<IPayOsPayoutService, PayOsPayoutService>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<PayOsSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
            });

            // Bind PayoutOptions from the PayOs config section so Application layer
            // can read AutoPayoutEnabled and PayoutCategory without depending on Infrastructure.
            services.Configure<SporticoApp.Application.Options.PayoutOptions>(options =>
            {
                var section = configuration.GetSection("PayOs");
                options.AutoPayoutEnabled = section.GetValue<bool>("AutoPayoutEnabled");
                options.PayoutCategory = section.GetValue<string>("PayoutCategory") ?? "salary";
            });

            return services;
        }
    }
}
