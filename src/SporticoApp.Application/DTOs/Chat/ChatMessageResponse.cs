namespace SporticoApp.Application.DTOs.Chat
{
    public class ChatMessageResponse
    {
        public Guid Id { get; set; }

        public Guid RoomId { get; set; }

        public Guid SenderId { get; set; }

        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime SentAt { get; set; }

        public List<ChatMessageAttachmentResponse> Attachments { get; set; } = new();
    }

    public class ChatMessageAttachmentResponse
    {
        public Guid Id { get; set; }

        public string FileUrl { get; set; } = string.Empty;

        public string? FileType { get; set; }
    }
}
