namespace SporticoApp.Application.DTOs.Bookings
{
    public class BookingResponse
    {
        public Guid Id { get; set; }

        public Guid LearnerId { get; set; }

        public Guid CoachId { get; set; }

        public Guid TrainingPackageId { get; set; }

        public string TrainingPackageTitle { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal PlatformFeeRate { get; set; }

        public decimal PlatformFeeAmount { get; set; }

        public decimal CoachReceiveAmount { get; set; }

        public decimal PerSessionCoachAmount { get; set; }

        public int TotalSessions { get; set; }

        /// <summary>Sessions actually completed (real count from TrainingSession rows).</summary>
        public int CompletedSessions { get; set; }

        /// <summary>
        /// Sessions consuming a quota slot: requested + scheduled + completed + missed
        /// (cancelled excluded). This — not CompletedSessions — governs whether more can be booked.
        /// </summary>
        public int UsedSessions { get; set; }

        /// <summary>Remaining bookable slots = max(0, TotalSessions − UsedSessions).</summary>
        public int RemainingSessions { get; set; }

        /// <summary>True when the package still has a free slot (RemainingSessions &gt; 0).</summary>
        public bool CanBookSession { get; set; }

        /// <summary>Raw count per session status (only non-zero statuses present).</summary>
        public IReadOnlyDictionary<string, int>? SessionCountsByStatus { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? PaidAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
