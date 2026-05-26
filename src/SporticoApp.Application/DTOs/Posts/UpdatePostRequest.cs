namespace SporticoApp.Application.DTOs.Posts
{
    public class UpdatePostRequest
    {
        public int SportId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        public List<string> ImageUrls { get; set; } = new();
    }
}
