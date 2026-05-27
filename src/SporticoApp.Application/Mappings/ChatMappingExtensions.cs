using SporticoApp.Application.DTOs.Chat;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class ChatMappingExtensions
    {
        public static ChatRoomResponse ToResponse(this ChatRoom room)
        {
            return new ChatRoomResponse
            {
                Id = room.Id,
                User1Id = room.User1Id,
                User2Id = room.User2Id,
                CreatedAt = room.CreatedAt
            };
        }

        public static ChatMessageResponse ToResponse(this Message message)
        {
            return new ChatMessageResponse
            {
                Id = message.Id,
                RoomId = message.RoomId,
                SenderId = message.SenderId,
                Content = message.Content,
                IsRead = message.IsRead,
                SentAt = message.SentAt
            };
        }
    }
}
