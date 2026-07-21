namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    public class VisitorStatsResponse
    {
        /// <summary>Distinct visitors (by VisitorId) with a session in the filter range (all-time if none given).</summary>
        public int TotalVisitors { get; set; }

        // The 3 windows below are always anchored to the current UTC instant, independent of the
        // FromDate/ToDate filter — matching the same "as of now" convention used by the payment
        // dashboard's RevenueToday/ThisWeek/ThisMonth fields.
        public int TodayVisitors { get; set; }

        public int WeeklyVisitors { get; set; }

        public int MonthlyVisitors { get; set; }

        /// <summary>Distinct visitors with a session touched within the last ActiveWindowMinutes.</summary>
        public int ActiveVisitors { get; set; }

        /// <summary>
        /// Distinct visitors with at least one NON-first session in range. Session-level bucket: a
        /// visitor whose first-ever visit AND a repeat visit both fall inside a wide range counts in
        /// both this and NewVisitors — the two are not a strict partition of "all visitors".
        /// </summary>
        public int ReturningVisitors { get; set; }

        /// <summary>Distinct visitors whose first-ever session falls in range.</summary>
        public int NewVisitors { get; set; }
    }
}
