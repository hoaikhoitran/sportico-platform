namespace SporticoApp.Application.DTOs.Chat
{
    public class ChatRoomResponse
    {
        public Guid Id { get; set; }

        public Guid User1Id { get; set; }

        public Guid User2Id { get; set; }

        /// <summary>The other participant relative to the current caller. Set only when the caller is known.</summary>
        public Guid? OtherUserId { get; set; }

        /// <summary>pending | active | rejected.</summary>
        public string Status { get; set; } = string.Empty;

        public Guid? RequestedByUserId { get; set; }

        public DateTime? RequestedAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? RejectedAt { get; set; }

        public DateTime? LastMessageAt { get; set; }

        public string? SourceType { get; set; }

        public Guid? SourceId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
