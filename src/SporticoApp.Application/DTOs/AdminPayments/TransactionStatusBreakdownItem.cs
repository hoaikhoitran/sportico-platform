namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>
    /// True distribution of Payment.Status values in range (pending | paid | failed | cancelled),
    /// with the "paid" bucket split into successful vs refunded so the chart mirrors the
    /// Statistics endpoint's RefundedTransactions definition.
    /// </summary>
    public class TransactionStatusBreakdownItem
    {
        public string Status { get; set; } = string.Empty;

        public int Count { get; set; }

        /// <summary>Share of Count across all statuses in range, 0..100.</summary>
        public decimal Percentage { get; set; }
    }
}
