namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    public class VisitorsChartPoint
    {
        public DateTime PeriodStart { get; set; }

        public string PeriodLabel { get; set; } = string.Empty;

        /// <summary>Distinct visitors whose session started in this bucket.</summary>
        public int VisitorCount { get; set; }

        /// <summary>Page views recorded in this bucket.</summary>
        public int PageViewCount { get; set; }
    }
}
