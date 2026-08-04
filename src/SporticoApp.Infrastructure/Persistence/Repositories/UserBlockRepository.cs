using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class UserBlockRepository : IUserBlockRepository
    {
        private readonly AppDbContext _context;

        public UserBlockRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedUserId)
            => _context.UserBlocks.AsNoTracking()
                .AnyAsync(x => x.BlockerId == blockerId && x.BlockedUserId == blockedUserId);

        public Task<bool> IsBlockedEitherDirectionAsync(Guid userId1, Guid userId2)
            => _context.UserBlocks.AsNoTracking()
                .AnyAsync(x =>
                    (x.BlockerId == userId1 && x.BlockedUserId == userId2) ||
                    (x.BlockerId == userId2 && x.BlockedUserId == userId1));

        public Task<UserBlock?> GetAsync(Guid blockerId, Guid blockedUserId)
            => _context.UserBlocks
                .FirstOrDefaultAsync(x => x.BlockerId == blockerId && x.BlockedUserId == blockedUserId);

        public Task<List<UserBlock>> GetBlockedByUserAsync(Guid blockerId)
            => _context.UserBlocks.AsNoTracking()
                .Include(x => x.BlockedUser)
                .Where(x => x.BlockerId == blockerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

        public async Task AddAsync(UserBlock block)
        {
            _context.UserBlocks.Add(block);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(UserBlock block)
        {
            _context.UserBlocks.Remove(block);
            await _context.SaveChangesAsync();
        }
    }
}
