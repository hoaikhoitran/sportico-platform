using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Infrastructure.Persistence;
using SporticoApp.Infrastructure.Persistence.Configurations;
using SporticoApp.Infrastructure.Persistence.Repositories;
using SporticoApp.Infrastructure.Services;

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

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<ICoachRepository, CoachRepository>();
            services.AddScoped<ISportRepository, SportRepository>();

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddSingleton<ISlugGenerator, SlugGenerator>();

            return services;
        }
    }
}
