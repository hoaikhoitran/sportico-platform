namespace SporticoApp.Application.DTOs.Reviews
{
    /// <summary>Aggregated active-review stats for a coach (repository result).</summary>
    public class CoachRatingStats
    {
        public int TotalReviews { get; set; }

        public decimal AverageRating { get; set; }

        public int OneStar { get; set; }
        public int TwoStar { get; set; }
        public int ThreeStar { get; set; }
        public int FourStar { get; set; }
        public int FiveStar { get; set; }
    }
}
