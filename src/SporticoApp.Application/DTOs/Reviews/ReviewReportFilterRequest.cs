namespace SporticoApp.Application.DTOs.Reviews
{
    public class ReviewReportFilterRequest
    {
        /// <summary>pending | reviewing | resolved | rejected. Null = all.</summary>
        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
