using SporticoApp.Application.DTOs.Analytics;

namespace SporticoApp.Application.Interfaces.Services
{
    /// <summary>Lightweight, dependency-free User-Agent parser (no external service, no NuGet package).</summary>
    public interface IUserAgentParser
    {
        UserAgentInfo Parse(string? userAgent);
    }
}
