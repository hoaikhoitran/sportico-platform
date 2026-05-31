namespace SporticoApp.Application.DTOs.Reviews
{
    public class CoachReviewSummaryResponse
    {
        public Guid CoachId { get; set; }

        public decimal AverageRating { get; set; }

        public int TotalReviews { get; set; }

        /// <summary>Counts of active reviews per star level (1..5).</summary>
        public RatingBreakdown RatingBreakdown { get; set; } = new();
    }

    public class RatingBreakdown
    {
        public int OneStar { get; set; }
        public int TwoStar { get; set; }
        public int ThreeStar { get; set; }
        public int FourStar { get; set; }
        public int FiveStar { get; set; }
    }
}
