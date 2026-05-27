namespace SporticoApp.Application.DTOs.Chat
{
    public class ChatRoomResponse
    {
        public Guid Id { get; set; }

        public Guid User1Id { get; set; }

        public Guid User2Id { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
