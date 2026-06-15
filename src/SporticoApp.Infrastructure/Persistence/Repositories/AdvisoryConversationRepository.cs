using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class AdvisoryConversationRepository : IAdvisoryConversationRepository
    {
        private readonly AppDbContext _context;

        public AdvisoryConversationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdvisoryConversation?> GetByIdForUpdateAsync(Guid conversationId)
        {
            return await _context.AdvisoryConversations
                .FirstOrDefaultAsync(x => x.Id == conversationId);
        }

        public async Task<List<AdvisoryMessage>> GetRecentMessagesAsync(Guid conversationId, int limit)
        {
            var recent = await _context.AdvisoryMessages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync();

            // Return chronological order (oldest first) for model context.
            recent.Reverse();
            return recent;
        }

        public Task AddConversationWithoutSaveAsync(AdvisoryConversation conversation)
        {
            _context.AdvisoryConversations.Add(conversation);
            return Task.CompletedTask;
        }

        public Task AddMessageWithoutSaveAsync(AdvisoryMessage message)
        {
            _context.AdvisoryMessages.Add(message);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
