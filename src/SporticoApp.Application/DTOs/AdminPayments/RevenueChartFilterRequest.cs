namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>Filter for the revenue-over-time chart.</summary>
    public class RevenueChartFilterRequest
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        /// <summary>day | week | month | year. Defaults to day.</summary>
        public string? Granularity { get; set; }
    }
}
