namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>Revenue-only slice of <see cref="PaymentStatisticsResponse"/> for GET .../revenue.</summary>
    public class RevenueSummaryResponse
    {
        public decimal TotalRevenue { get; set; }

        public decimal PlatformRevenue { get; set; }

        public decimal CoachRevenue { get; set; }

        public decimal RevenueToday { get; set; }

        public decimal RevenueThisWeek { get; set; }

        public decimal RevenueThisMonth { get; set; }

        public decimal RevenueThisYear { get; set; }
    }
}
