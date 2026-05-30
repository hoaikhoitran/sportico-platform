namespace SporticoApp.Application.DTOs.Availability
{
    public class CoachAvailabilitySlotResponse
    {
        public Guid Id { get; set; }
        public Guid CoachId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? MeetingUrl { get; set; }
        public bool IsOnline { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
