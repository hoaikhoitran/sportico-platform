namespace SporticoApp.Application.DTOs.Availability
{
    public class CoachAvailabilitySlotResponse
    {
        public Guid Id { get; set; }
        public Guid CoachId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;

        /// <summary>Maximum learners that can book this slot.</summary>
        public int MaxParticipants { get; set; }

        /// <summary>Learners that have an active session on this slot (requested/scheduled/completed).</summary>
        public int BookedParticipants { get; set; }

        /// <summary>Remaining seats = MaxParticipants - BookedParticipants (never negative).</summary>
        public int RemainingParticipants { get; set; }

        /// <summary>True when no seats remain.</summary>
        public bool IsFull { get; set; }

        public string? Location { get; set; }
        public string? MeetingUrl { get; set; }
        public bool IsOnline { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
