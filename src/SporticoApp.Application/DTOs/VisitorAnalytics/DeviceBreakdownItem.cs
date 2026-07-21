namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    public class DeviceBreakdownItem
    {
        public string Device { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal Percentage { get; set; }
    }
}
