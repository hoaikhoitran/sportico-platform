namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>
    /// Headline payment KPIs for the admin dashboard. Revenue fields follow the SAME "paid booking"
    /// accounting rule as <see cref="SporticoApp.Application.DTOs.Dashboard.AdminDashboardResponse"/>
    /// (PaidAt set and Status is active/completed) so the two dashboards never disagree.
    /// </summary>
    public class PaymentStatisticsResponse
    {
        /// <summary>Sum of paid bookings' TotalAmount (what learners paid) within the filter range.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Sum of paid bookings' PlatformFeeAmount within the filter range.</summary>
        public decimal PlatformRevenue { get; set; }

        /// <summary>Sum of paid bookings' CoachReceiveAmount within the filter range.</summary>
        public decimal CoachRevenue { get; set; }

        /// <summary>Total Payment rows within the filter range (all statuses).</summary>
        public int TotalTransactions { get; set; }

        /// <summary>Payment.Status == paid, excluding ones whose booking was later refunded.</summary>
        public int SuccessfulTransactions { get; set; }

        /// <summary>Payment.Status == failed or cancelled.</summary>
        public int FailedTransactions { get; set; }

        /// <summary>Payment.Status == pending.</summary>
        public int PendingTransactions { get; set; }

        /// <summary>
        /// Paid payments whose linked booking has Status == refunded. There is currently no
        /// automated refund flow that sets this status, so this is always 0 today — the field is
        /// wired correctly and forward-compatible with a future refund feature.
        /// </summary>
        public int RefundedTransactions { get; set; }

        /// <summary>Average Payment.Amount over successful (paid) transactions in range.</summary>
        public decimal AverageTransactionValue { get; set; }

        // The four "as of now" windows below are always anchored to the current UTC instant —
        // they do NOT depend on the FromDate/ToDate range filter, matching typical dashboard
        // semantics ("today's revenue" doesn't change when you pick a custom report range).
        public decimal RevenueToday { get; set; }

        public decimal RevenueThisWeek { get; set; }

        public decimal RevenueThisMonth { get; set; }

        public decimal RevenueThisYear { get; set; }
    }
}
