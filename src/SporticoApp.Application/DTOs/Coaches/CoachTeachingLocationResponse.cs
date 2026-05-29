using System;

namespace SporticoApp.Application.DTOs.Coaches
{
    public class CoachTeachingLocationResponse
    {
        public Guid Id { get; set; }

        public Guid CoachId { get; set; }

        public string Address { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? District { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
