namespace SporticoApp.Application.DTOs.Reviews
{
    public class ResolveReviewReportRequest
    {
        /// <summary>True if the admin judges the report valid (the review violates policy).</summary>
        public bool IsValid { get; set; }

        public string? ResolutionNote { get; set; }

        /// <summary>
        /// When the report is valid, hide (or delete) the offending review and recalculate
        /// the coach's rating. Ignored when <see cref="IsValid"/> is false.
        /// </summary>
        public bool HideOrDeleteReview { get; set; }
    }
}
