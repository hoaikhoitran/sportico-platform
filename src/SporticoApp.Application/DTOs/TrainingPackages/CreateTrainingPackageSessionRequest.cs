namespace SporticoApp.Application.DTOs.TrainingPackages
{
    /// <summary>
    /// One scheduled session inside a training package, supplied by the coach during package
    /// creation/update. The same shape is reused for create and update — the whole schedule is
    /// always sent as a full set of exactly <c>SessionCount</c> items.
    /// </summary>
    public class CreateTrainingPackageSessionRequest
    {
        /// <summary>1..SessionCount — must be unique within the package.</summary>
        public int SessionNumber { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string? Level { get; set; }

        /// <summary>Maximum learners that can buy a seat on this session. Must be greater than 0.</summary>
        public int MaxParticipants { get; set; } = 1;

        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        public string? MeetingUrl { get; set; }

        public string? Note { get; set; }
    }
}
