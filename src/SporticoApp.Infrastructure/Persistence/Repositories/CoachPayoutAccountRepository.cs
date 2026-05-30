using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CoachPayoutAccountRepository : ICoachPayoutAccountRepository
    {
        private readonly AppDbContext _context;

        public CoachPayoutAccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CoachPayoutAccount?> GetByCoachIdAsync(Guid coachId)
        {
            return await _context.CoachPayoutAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CoachId == coachId);
        }

        public async Task<CoachPayoutAccount?> GetByCoachIdForUpdateAsync(Guid coachId)
        {
            return await _context.CoachPayoutAccounts
                .FirstOrDefaultAsync(x => x.CoachId == coachId);
        }

        public async Task<CoachPayoutAccount?> GetByIdAsync(Guid id)
        {
            return await _context.CoachPayoutAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<CoachPayoutAccount?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.CoachPayoutAccounts
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(List<CoachPayoutAccount> Items, int TotalCount)> GetPendingPagedAsync(
            int pageNumber,
            int pageSize)
        {
            var query = _context.CoachPayoutAccounts
                .AsNoTracking()
                .Where(x => x.Status == PayoutAccountStatuses.Pending);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(CoachPayoutAccount account)
        {
            _context.CoachPayoutAccounts.Add(account);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
