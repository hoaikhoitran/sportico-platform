namespace SporticoApp.Application.DTOs.Dashboard
{
    public class CoachDashboardResponse
    {
        public Guid CoachId { get; set; }

        // Bookings
        public int ActiveBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }

        // Sessions
        public int RequestedSessions { get; set; }
        public int UpcomingSessions { get; set; }   // scheduled and in the future
        public int CompletedSessions { get; set; }
        public int CancelledSessions { get; set; }
        /// <summary>completed / (completed + cancelled); 0 when there are none.</summary>
        public decimal SessionCompletionRate { get; set; }

        // Wallet (point-in-time snapshot)
        public decimal TotalEarned { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal TotalWithdrawn { get; set; }

        // Withdrawals
        public int PendingWithdrawalRequests { get; set; }   // pending + processing
    }
}
