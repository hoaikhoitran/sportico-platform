using Microsoft.Extensions.DependencyInjection;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Services;

namespace SporticoApp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationDI(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            return services;
        }
    }
}
