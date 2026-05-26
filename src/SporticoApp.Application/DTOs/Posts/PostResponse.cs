namespace SporticoApp.Application.DTOs.Posts
{
    public class PostResponse
    {
        public Guid Id { get; set; }

        public Guid CoachId { get; set; }

        public int SportId { get; set; }

        public string SportName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<string> ImageUrls { get; set; } = new();
    }
}
