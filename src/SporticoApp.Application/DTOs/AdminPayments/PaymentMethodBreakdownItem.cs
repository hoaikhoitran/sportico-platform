namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>Payment volume/amount grouped by gateway method (payos | manual — vnpay if ever added).</summary>
    public class PaymentMethodBreakdownItem
    {
        public string Method { get; set; } = string.Empty;

        public int TransactionCount { get; set; }

        public decimal TotalAmount { get; set; }

        /// <summary>Share of TransactionCount across all methods in range, 0..100.</summary>
        public decimal Percentage { get; set; }
    }
}
