namespace SporticoApp.Application.DTOs.Reviews
{
    public class ReviewReportResponse
    {
        public Guid Id { get; set; }

        public Guid ReporterId { get; set; }

        public Guid ReviewId { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? ActionTaken { get; set; }

        public Guid? HandledByUserId { get; set; }

        public DateTime? HandledAt { get; set; }

        public string? ResolutionNote { get; set; }

        public DateTime CreatedAt { get; set; }

        // ── Snapshot of the reported review (for the admin moderation queue) ──
        public int? ReviewRating { get; set; }

        public string? ReviewComment { get; set; }

        public string? ReviewStatus { get; set; }

        public Guid? ReviewCoachId { get; set; }

        public Guid? ReviewLearnerId { get; set; }
    }
}
