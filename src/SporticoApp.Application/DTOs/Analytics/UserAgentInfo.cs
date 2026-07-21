using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.DTOs.Analytics
{
    /// <summary>Result of parsing a raw User-Agent header.</summary>
    public class UserAgentInfo
    {
        public string Device { get; set; } = DeviceTypes.Unknown;

        public string Browser { get; set; } = "Unknown";

        public string Os { get; set; } = "Unknown";
    }
}
