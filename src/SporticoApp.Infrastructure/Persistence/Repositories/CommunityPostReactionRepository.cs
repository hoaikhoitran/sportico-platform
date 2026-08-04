using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CommunityPostReactionRepository : ICommunityPostReactionRepository
    {
        private readonly AppDbContext _context;

        public CommunityPostReactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<CommunityPostReaction?> GetAsync(Guid postId, Guid userId)
        {
            return _context.CommunityPostReactions
                .FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);
        }

        public Task AddWithoutSaveAsync(CommunityPostReaction reaction)
        {
            _context.CommunityPostReactions.Add(reaction);
            return Task.CompletedTask;
        }

        public Task RemoveWithoutSaveAsync(CommunityPostReaction reaction)
        {
            _context.CommunityPostReactions.Remove(reaction);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
