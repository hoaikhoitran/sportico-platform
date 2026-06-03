namespace SporticoApp.Application.DTOs.Availability
{
    public class CreateCoachAvailabilitySlotRequest
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public string? MeetingUrl { get; set; }
        public bool IsOnline { get; set; }
        public string? Note { get; set; }

        /// <summary>
        /// Maximum learners that can book this slot (group slot). Optional — defaults to 1 (private
        /// slot) when omitted. Valid range 1..50.
        /// </summary>
        public int? MaxParticipants { get; set; }
    }
}
