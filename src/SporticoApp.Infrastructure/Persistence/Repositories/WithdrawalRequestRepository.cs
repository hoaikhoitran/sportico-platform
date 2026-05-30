using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Withdrawals;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class WithdrawalRequestRepository : IWithdrawalRequestRepository
    {
        private readonly AppDbContext _context;

        public WithdrawalRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<WithdrawalRequest?> GetByIdAsync(Guid id)
        {
            return await _context.WithdrawalRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<WithdrawalRequest?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.WithdrawalRequests
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPagedByCoachAsync(
            Guid coachId,
            WithdrawalRequestFilterRequest filter)
        {
            IQueryable<WithdrawalRequest> query = _context.WithdrawalRequests
                .AsNoTracking()
                .Where(x => x.CoachId == coachId);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPendingPagedAsync(
            WithdrawalRequestFilterRequest filter)
        {
            IQueryable<WithdrawalRequest> query = _context.WithdrawalRequests
                .AsNoTracking()
                .Where(x => x.Status == WithdrawalRequestStatuses.Pending);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(WithdrawalRequest request)
        {
            _context.WithdrawalRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public Task AddWithoutSaveAsync(WithdrawalRequest request)
        {
            _context.WithdrawalRequests.Add(request);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // CoachWallet uses xmin optimistic concurrency; two concurrent withdrawals for
                // the same coach will collide here. Surface it as ConflictException so the
                // Application layer can return a 409 without referencing EF Core directly.
                throw new ConflictException(
                    ErrorCodes.InsufficientWalletBalance,
                    "A concurrent withdrawal was processed. Please check your balance and try again.");
            }
        }

        private static IQueryable<WithdrawalRequest> ApplyFilter(
            IQueryable<WithdrawalRequest> query,
            WithdrawalRequestFilterRequest filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalized = filter.Status.Trim().ToLowerInvariant();
                query = query.Where(x => x.Status.ToLower() == normalized);
            }

            return query;
        }
    }
}
