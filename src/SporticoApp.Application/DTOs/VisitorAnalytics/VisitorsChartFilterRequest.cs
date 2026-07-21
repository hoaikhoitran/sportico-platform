namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    public class VisitorsChartFilterRequest
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        /// <summary>hour | day | week | month | year. Defaults to day.</summary>
        public string? Granularity { get; set; }
    }
}
