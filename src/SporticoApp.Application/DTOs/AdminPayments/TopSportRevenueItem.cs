namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>One row of the top-sports-by-revenue leaderboard, over paid bookings in range.</summary>
    public class TopSportRevenueItem
    {
        public int SportId { get; set; }

        public string SportName { get; set; } = string.Empty;

        public decimal TotalRevenue { get; set; }

        public int BookingCount { get; set; }
    }
}
