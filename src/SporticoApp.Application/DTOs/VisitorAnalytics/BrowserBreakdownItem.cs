namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    public class BrowserBreakdownItem
    {
        public string Browser { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal Percentage { get; set; }
    }
}
