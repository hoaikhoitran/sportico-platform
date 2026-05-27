using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Wallets;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CoachWalletRepository : ICoachWalletRepository
    {
        private readonly AppDbContext _context;

        public CoachWalletRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CoachWallet?> GetByCoachIdAsync(Guid coachId)
        {
            return await _context.CoachWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CoachId == coachId);
        }

        public async Task<CoachWallet?> GetByCoachIdForUpdateAsync(Guid coachId)
        {
            return await _context.CoachWallets
                .FirstOrDefaultAsync(x => x.CoachId == coachId);
        }

        public async Task<(List<CoachWalletTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(
            Guid coachId,
            CoachWalletTransactionFilterRequest filter)
        {
            IQueryable<CoachWalletTransaction> query = _context.CoachWalletTransactions
                .AsNoTracking()
                .Where(x => x.CoachId == coachId);

            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                var normalized = filter.Type.Trim().ToLowerInvariant();
                query = query.Where(x => x.Type.ToLower() == normalized);
            }

            if (!string.IsNullOrWhiteSpace(filter.Direction))
            {
                var normalized = filter.Direction.Trim().ToLowerInvariant();
                query = query.Where(x => x.Direction.ToLower() == normalized);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(CoachWallet wallet)
        {
            _context.CoachWallets.Add(wallet);
            await _context.SaveChangesAsync();
        }

        public Task AddWithoutSaveAsync(CoachWallet wallet)
        {
            _context.CoachWallets.Add(wallet);
            return Task.CompletedTask;
        }

        public Task AddTransactionWithoutSaveAsync(CoachWalletTransaction transaction)
        {
            _context.CoachWalletTransactions.Add(transaction);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
