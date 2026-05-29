using System;

namespace SporticoApp.Application.DTOs.Coaches
{
    public class CoachProfileMediaResponse
    {
        public Guid Id { get; set; }

        public Guid CoachId { get; set; }

        public string MediaType { get; set; } = string.Empty;

        public string MediaUrl { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string? Description { get; set; }

        public int OrderIndex { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
