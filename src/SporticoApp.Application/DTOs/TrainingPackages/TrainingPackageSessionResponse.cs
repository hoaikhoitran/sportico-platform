namespace SporticoApp.Application.DTOs.TrainingPackages
{
    public class TrainingPackageSessionResponse
    {
        public Guid Id { get; set; }

        public int SessionNumber { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string? Level { get; set; }

        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        public string? MeetingUrl { get; set; }

        public string? Note { get; set; }

        public int MaxParticipants { get; set; }

        public int BookedParticipants { get; set; }

        /// <summary>Remaining seats = max(0, MaxParticipants − BookedParticipants).</summary>
        public int RemainingParticipants { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
