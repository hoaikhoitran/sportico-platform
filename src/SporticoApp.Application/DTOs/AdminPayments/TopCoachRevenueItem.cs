namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>One row of the top-coaches-by-revenue leaderboard, over paid bookings in range.</summary>
    public class TopCoachRevenueItem
    {
        public Guid CoachId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        /// <summary>Gross sales volume the coach generated (sum of TotalAmount).</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>What the coach actually earned (sum of CoachReceiveAmount).</summary>
        public decimal CoachEarnings { get; set; }

        /// <summary>Platform fee generated from this coach's sales.</summary>
        public decimal PlatformFeeGenerated { get; set; }

        public int BookingCount { get; set; }
    }
}
