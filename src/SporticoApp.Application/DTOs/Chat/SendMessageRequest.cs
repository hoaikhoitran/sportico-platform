namespace SporticoApp.Application.DTOs.Chat
{
    public class SendMessageRequest
    {
        /// <summary>Optional when at least one attachment is provided.</summary>
        public string? Content { get; set; }

        public List<SendMessageAttachmentRequest>? Attachments { get; set; }
    }

    public class SendMessageAttachmentRequest
    {
        public string FileUrl { get; set; } = string.Empty;

        public string? FileType { get; set; }
    }
}
