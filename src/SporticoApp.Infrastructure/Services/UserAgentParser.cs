using System.Text.RegularExpressions;
using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Services
{
    /// <summary>
    /// Regex-based, dependency-free User-Agent parser — good enough for analytics device/browser/OS
    /// breakdowns without adding a third-party UA-parsing NuGet package or external service.
    /// </summary>
    public class UserAgentParser : IUserAgentParser
    {
        private static readonly Regex BotRegex = new(
            @"bot|crawl|spider|slurp|facebookexternalhit|whatsapp|telegrambot|googlebot|bingbot|yandexbot|duckduckbot|baiduspider",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TabletRegex = new(
            @"ipad|tablet|kindle|playbook",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex AndroidRegex = new(@"android", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MobileRegex = new(
            @"mobile|iphone|ipod|blackberry|iemobile|opera mini|windows phone",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public UserAgentInfo Parse(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return new UserAgentInfo();
            }

            return new UserAgentInfo
            {
                Device = DetectDevice(userAgent),
                Browser = DetectBrowser(userAgent),
                Os = DetectOs(userAgent)
            };
        }

        private static string DetectDevice(string ua)
        {
            if (BotRegex.IsMatch(ua)) return DeviceTypes.Bot;
            if (TabletRegex.IsMatch(ua)) return DeviceTypes.Tablet;
            // Android without the "Mobile" token is conventionally a tablet.
            if (AndroidRegex.IsMatch(ua) && !ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase)) return DeviceTypes.Tablet;
            if (MobileRegex.IsMatch(ua) || AndroidRegex.IsMatch(ua)) return DeviceTypes.Mobile;
            return DeviceTypes.Desktop;
        }

        private static string DetectBrowser(string ua)
        {
            // Order matters: Edge/Opera/Chrome UAs also contain "Safari"/"Chrome" tokens.
            if (ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) return "Edge";
            if (ua.Contains("OPR/", StringComparison.OrdinalIgnoreCase) || ua.Contains("Opera", StringComparison.OrdinalIgnoreCase)) return "Opera";
            if (ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase) && !ua.Contains("Chromium", StringComparison.OrdinalIgnoreCase)) return "Chrome";
            if (ua.Contains("Firefox", StringComparison.OrdinalIgnoreCase)) return "Firefox";
            if (ua.Contains("Safari", StringComparison.OrdinalIgnoreCase) && !ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase)) return "Safari";
            if (ua.Contains("MSIE", StringComparison.OrdinalIgnoreCase) || ua.Contains("Trident", StringComparison.OrdinalIgnoreCase)) return "Internet Explorer";
            return "Unknown";
        }

        private static string DetectOs(string ua)
        {
            // iOS/Android checks come first: their UAs also contain "like Mac OS X" / "Linux".
            if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("iPod", StringComparison.OrdinalIgnoreCase)) return "iOS";
            if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase)) return "Android";
            if (ua.Contains("Windows", StringComparison.OrdinalIgnoreCase)) return "Windows";
            if (ua.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase) || ua.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)) return "macOS";
            if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "Linux";
            return "Unknown";
        }
    }
}
