namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>
    /// Single composite payload for GET /api/admin/payments/dashboard — everything an admin
    /// payment-dashboard screen needs in one round trip. Every section reuses the same
    /// repository queries backing the dedicated single-purpose endpoints (no duplicated logic).
    /// </summary>
    public class AdminPaymentDashboardResponse
    {
        public PaymentStatisticsResponse Statistics { get; set; } = new();

        public List<RevenueChartPoint> RevenueChart { get; set; } = new();

        public List<PaymentMethodBreakdownItem> PaymentMethodBreakdown { get; set; } = new();

        public List<TransactionStatusBreakdownItem> TransactionStatusBreakdown { get; set; } = new();

        public List<TopCoachRevenueItem> TopCoaches { get; set; } = new();

        public List<TopSportRevenueItem> TopSports { get; set; } = new();

        public List<AdminTransactionResponse> RecentTransactions { get; set; } = new();
    }
}
