namespace SporticoApp.Application.DTOs.Coaches
{
    public class CreateCoachTeachingLocationRequest
    {
        public string Address { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? District { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public bool IsDefault { get; set; }
    }
}
