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

        public int CompletedSessions { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? PaidAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
