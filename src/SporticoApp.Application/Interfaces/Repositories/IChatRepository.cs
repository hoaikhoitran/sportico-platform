using SporticoApp.Application.DTOs.Chat;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IChatRepository
    {
        Task<ChatRoom?> GetRoomByIdAsync(Guid roomId);

        Task<ChatRoom?> GetRoomByIdForUpdateAsync(Guid roomId);

        Task<ChatRoom?> GetRoomByUsersAsync(Guid userId1, Guid userId2);

        Task<List<ChatRoom>> GetRoomsForUserAsync(Guid userId);

        Task<(List<Message> Items, int TotalCount)> GetMessagesByRoomAsync(
            Guid roomId,
            ChatMessageFilterRequest filter);

        /// <summary>Returns the inserted room, or the existing room if a concurrent insert caused a unique-key conflict.</summary>
        Task<ChatRoom> AddRoomAsync(ChatRoom room);

        Task AddMessageAsync(Message message);

        Task AddMessageWithoutSaveAsync(Message message);

        Task AddAttachmentsWithoutSaveAsync(IEnumerable<MessageAttachment> attachments);

        Task SaveChangesAsync();
    }
}
