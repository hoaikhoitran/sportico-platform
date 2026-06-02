namespace SporticoApp.Application.DTOs.Dashboard
{
    public class AdminDashboardResponse
    {
        // Users
        public int TotalUsers { get; set; }
        public int TotalLearners { get; set; }
        public int TotalCoaches { get; set; }

        // Catalog
        public int PublishedPackages { get; set; }

        // Bookings
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }

        // Accounting (over PAID bookings — status active or completed, PaidAt set)
        /// <summary>Gross revenue = sum of booking total amounts paid by learners.</summary>
        public decimal GrossRevenue { get; set; }
        /// <summary>Platform revenue = sum of the 15% platform fee snapshots.</summary>
        public decimal PlatformFeeRevenue { get; set; }
        /// <summary>Coach earning liability = sum of coach-receive snapshots (85%).</summary>
        public decimal CoachPayable { get; set; }
        /// <summary>Sum of amounts on withdrawals already paid out.</summary>
        public decimal TotalWithdrawnPaid { get; set; }

        // Withdrawals by status
        public int PendingWithdrawals { get; set; }
        public int ProcessingWithdrawals { get; set; }
        public int PaidWithdrawals { get; set; }
        public int FailedWithdrawals { get; set; }
    }
}
