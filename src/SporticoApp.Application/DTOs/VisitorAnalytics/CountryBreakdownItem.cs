namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    public class CountryBreakdownItem
    {
        /// <summary>"Unknown" when no reverse-proxy geo header was present on any session in the bucket.</summary>
        public string Country { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal Percentage { get; set; }
    }
}
