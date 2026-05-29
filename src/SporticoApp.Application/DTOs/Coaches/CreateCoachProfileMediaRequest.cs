namespace SporticoApp.Application.DTOs.Coaches
{
    public class CreateCoachProfileMediaRequest
    {
        public string MediaType { get; set; } = string.Empty;

        public string MediaUrl { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string? Description { get; set; }

        public int OrderIndex { get; set; }
    }
}
