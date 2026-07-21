namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>One bucket of the revenue-over-time chart (day/week/month/year granularity).</summary>
    public class RevenueChartPoint
    {
        /// <summary>Start of the bucket (UTC), e.g. the day/ISO-week-start/month-start/year-start.</summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>Human-readable label for the bucket, e.g. "2026-07-17", "2026-07", "2026".</summary>
        public string PeriodLabel { get; set; } = string.Empty;

        public decimal GrossRevenue { get; set; }

        public decimal PlatformRevenue { get; set; }

        public decimal CoachRevenue { get; set; }

        public int TransactionCount { get; set; }
    }
}
