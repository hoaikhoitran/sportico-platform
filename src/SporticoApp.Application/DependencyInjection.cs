using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Coaches;
using SporticoApp.Application.Validators.Sports;

namespace SporticoApp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationDI(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICoachService, CoachService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<ISportService, SportService>();

            services.AddValidatorsFromAssemblyContaining
                <RegisterCoachRequestValidator>();

            services.AddValidatorsFromAssemblyContaining
                <CreateSportRequestValidator>();

            services.AddValidatorsFromAssemblyContaining
                <CreateSportRequestValidator>();

            return services;
        }
    }
}
