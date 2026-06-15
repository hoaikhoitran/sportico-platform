using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IAdvisoryConversationRepository
    {
        /// <summary>Tracked fetch for ownership checks and bumping <c>UpdatedAt</c>.</summary>
        Task<AdvisoryConversation?> GetByIdForUpdateAsync(Guid conversationId);

        /// <summary>Most recent messages of a conversation, returned in chronological order.</summary>
        Task<List<AdvisoryMessage>> GetRecentMessagesAsync(Guid conversationId, int limit);

        Task AddConversationWithoutSaveAsync(AdvisoryConversation conversation);

        Task AddMessageWithoutSaveAsync(AdvisoryMessage message);

        Task SaveChangesAsync();
    }
}
