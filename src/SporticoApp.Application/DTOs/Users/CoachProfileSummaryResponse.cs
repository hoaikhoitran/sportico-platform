namespace SporticoApp.Application.DTOs.Users
{
    public class CoachProfileSummaryResponse
    {
        public string? Headline { get; set; }

        public string? Bio { get; set; }

        public int? ExperienceYears { get; set; }

        public string? CoverImageUrl { get; set; }

        public decimal Rating { get; set; }

        public int TotalReviews { get; set; }
    }
}
