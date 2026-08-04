using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class VoucherRedemptionRepository : IVoucherRedemptionRepository
    {
        private readonly AppDbContext _context;

        public VoucherRedemptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VoucherRedemption?> GetByBookingIdForUpdateAsync(Guid bookingId)
        {
            return await _context.VoucherRedemptions
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);
        }

        public async Task<int> CountByLearnerAndCampaignAsync(Guid learnerId, Guid campaignId, IReadOnlyCollection<string> statuses)
        {
            return await _context.VoucherRedemptions
                .AsNoTracking()
                .Where(x => x.LearnerId == learnerId && x.VoucherCampaignId == campaignId && statuses.Contains(x.Status))
                .CountAsync();
        }

        public async Task<(List<VoucherRedemption> Items, int TotalCount)> GetPagedByCampaignAsync(
            Guid campaignId, VoucherRedemptionFilterRequest filter)
        {
            IQueryable<VoucherRedemption> query = _context.VoucherRedemptions
                .AsNoTracking()
                .Where(x => x.VoucherCampaignId == campaignId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(x => x.Status == filter.Status);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.ReservedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<VoucherRedemption>> GetExpiredReservedAsync(DateTime nowUtc, int batchSize)
        {
            return await _context.VoucherRedemptions
                .Where(x =>
                    x.Status == VoucherRedemptionStatuses.Reserved &&
                    x.ExpiresAt != null &&
                    x.ExpiresAt < nowUtc)
                .OrderBy(x => x.ExpiresAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public Task AddWithoutSaveAsync(VoucherRedemption redemption)
        {
            _context.VoucherRedemptions.Add(redemption);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
