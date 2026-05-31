namespace SporticoApp.Application.DTOs.Reviews
{
    public class CreateReviewReportRequest
    {
        public string Reason { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
