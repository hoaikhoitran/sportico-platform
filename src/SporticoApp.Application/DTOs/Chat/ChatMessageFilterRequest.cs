namespace SporticoApp.Application.DTOs.Chat
{
    public class ChatMessageFilterRequest
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
