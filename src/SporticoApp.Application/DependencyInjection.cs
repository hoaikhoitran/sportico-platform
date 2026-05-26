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
            services.AddScoped<ICoachService, CoachService>();
            services.AddScoped<ICoachPackageService, CoachPackageService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IAdminPostService, AdminPostService>();
            services.AddScoped<ISportService, SportService>();

            services.AddValidatorsFromAssemblyContaining
                <RegisterCoachRequestValidator>();

            return services;
        }
    }
}
