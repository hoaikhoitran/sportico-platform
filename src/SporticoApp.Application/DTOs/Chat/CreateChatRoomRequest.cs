namespace SporticoApp.Application.DTOs.Chat
{
    public class CreateChatRoomRequest
    {
        /// <summary>Preferred field — any active user, not just a coach.</summary>
        public Guid? TargetUserId { get; set; }

        /// <summary>Legacy field, still accepted for backward compatibility. Ignored when TargetUserId is set.</summary>
        public Guid? CoachId { get; set; }

        /// <summary>Optional context this room was opened from, e.g. "community_post".</summary>
        public string? SourceType { get; set; }

        public Guid? SourceId { get; set; }
    }
}
