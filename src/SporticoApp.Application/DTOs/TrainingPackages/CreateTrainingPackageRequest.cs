using System.Collections.Generic;

namespace SporticoApp.Application.DTOs.TrainingPackages
{
    public class CreateTrainingPackageRequest
    {
        public int SportId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int SessionCount { get; set; }

        /// <summary>First calendar day the schedule may span. Replaces the old duration/month model.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Last calendar day the schedule may span.</summary>
        public DateTime EndDate { get; set; }

        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        public string? Level { get; set; }

        public string? GoalType { get; set; }

        /// <summary>Full schedule of sessions. Must contain exactly <see cref="SessionCount"/> items.</summary>
        public List<CreateTrainingPackageSessionRequest> Sessions { get; set; } = new();
    }
}
