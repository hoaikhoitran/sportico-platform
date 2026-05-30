using SporticoApp.Application.DTOs.Chat;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IChatService
    {
        Task<Result<ChatRoomResponse>> CreateOrGetRoomAsync(Guid userId, CreateChatRoomRequest request);

        Task<Result<List<ChatRoomResponse>>> GetRoomsAsync(Guid userId);

        Task<Result<PagedResult<ChatMessageResponse>>> GetMessagesAsync(
            Guid userId,
            Guid roomId,
            ChatMessageFilterRequest filter);

        Task<Result<ChatMessageResponse>> SendMessageAsync(
            Guid userId,
            Guid roomId,
            SendMessageRequest request);
    }
}
