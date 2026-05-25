namespace SporticoApp.Application.DTOs.Sports
{
    public class CreateSportRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Slug { get; set; }

        public string? Description { get; set; }

        public string? IconUrl { get; set; }
    }
}
