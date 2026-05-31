namespace SporticoApp.Application.DTOs.Reviews
{
    public class ReviewResponse
    {
        public Guid Id { get; set; }

        public Guid CoachId { get; set; }

        public string CoachName { get; set; } = string.Empty;

        public Guid LearnerId { get; set; }

        public string LearnerName { get; set; } = string.Empty;

        public string? LearnerAvatarUrl { get; set; }

        public Guid? BookingId { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// True only when the requesting user owns the review AND still has a non-expired
        /// successful booking with the coach. Always false for anonymous/other users.
        /// </summary>
        public bool CanEdit { get; set; }
    }
}
