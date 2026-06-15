namespace SporticoApp.Application.DTOs.Advisory
{
    public class SendAdvisoryMessageRequest
    {
        /// <summary>
        /// Existing conversation to continue. Leave null to start a new conversation.
        /// </summary>
        public Guid? ConversationId { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
