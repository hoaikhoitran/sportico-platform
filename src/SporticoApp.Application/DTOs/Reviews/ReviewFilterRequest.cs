namespace SporticoApp.Application.DTOs.Reviews
{
    public class ReviewFilterRequest
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        /// <summary>Optional star filter (1..5).</summary>
        public int? Rating { get; set; }

        /// <summary>latest | highest | lowest. Defaults to latest.</summary>
        public string? SortBy { get; set; }
    }
}
