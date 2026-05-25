using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.DTOs.Coaches
{
    public class CoachProfileResponse
    {
        public Guid UserId { get; set; }

        public string? Headline { get; set; }

        public string? Bio { get; set; }

        public int? ExperienceYears { get; set; }

        public decimal Rating { get; set; }

        public int TotalReviews { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
