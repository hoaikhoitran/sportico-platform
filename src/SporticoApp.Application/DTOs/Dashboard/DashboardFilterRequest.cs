namespace SporticoApp.Application.DTOs.Dashboard
{
    /// <summary>
    /// Optional date range for dashboard time-bounded metrics. Bounds bookings/payments by
    /// CreatedAt, sessions by StartTime and withdrawals by CreatedAt. Wallet balances are a
    /// point-in-time snapshot and are not date-filtered.
    /// </summary>
    public class DashboardFilterRequest
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
