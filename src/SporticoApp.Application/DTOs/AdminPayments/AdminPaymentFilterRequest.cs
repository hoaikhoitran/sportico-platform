namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>Filter/sort/paging for the admin transactions list and recent-transactions feed.</summary>
    public class AdminPaymentFilterRequest
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        /// <summary>pending | paid | failed | cancelled (Payment.Status values).</summary>
        public string? Status { get; set; }

        /// <summary>payos | manual (Payment.Method values).</summary>
        public string? Method { get; set; }

        /// <summary>newest | oldest | amount_desc | amount_asc. Defaults to newest.</summary>
        public string? SortBy { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
