namespace SporticoApp.Application.DTOs.Posts
{
    public class PostFilterRequest
    {
        public string? Keyword { get; set; }

        public string? Status { get; set; }

        public int? SportId { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
