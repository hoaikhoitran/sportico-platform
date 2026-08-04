using SporticoApp.Application.DTOs.Chat;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class ChatMappingExtensions
    {
        public static ChatRoomResponse ToResponse(this ChatRoom room, Guid? currentUserId = null)
        {
            return new ChatRoomResponse
            {
                Id = room.Id,
                User1Id = room.User1Id,
                User2Id = room.User2Id,
                OtherUserId = currentUserId.HasValue
                    ? (room.User1Id == currentUserId.Value ? room.User2Id : room.User1Id)
                    : null,
                Status = room.Status,
                RequestedByUserId = room.RequestedByUserId,
                RequestedAt = room.RequestedAt,
                AcceptedAt = room.AcceptedAt,
                RejectedAt = room.RejectedAt,
                LastMessageAt = room.LastMessageAt,
                SourceType = room.SourceType,
                SourceId = room.SourceId,
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
                SentAt = message.SentAt,
                Attachments = message.MessageAttachments
                    .Select(a => new ChatMessageAttachmentResponse
                    {
                        Id = a.Id,
                        FileUrl = a.FileUrl,
                        FileType = a.FileType
                    })
                    .ToList()
            };
        }
    }
}
