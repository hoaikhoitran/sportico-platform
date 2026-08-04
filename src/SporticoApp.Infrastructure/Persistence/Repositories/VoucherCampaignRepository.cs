using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class VoucherCampaignRepository : IVoucherCampaignRepository
    {
        private readonly AppDbContext _context;

        public VoucherCampaignRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VoucherCampaign?> GetByCodeAsync(string code)
        {
            return await _context.VoucherCampaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code);
        }

        public async Task<VoucherCampaign?> GetByCodeForUpdateAsync(string code)
        {
            return await _context.VoucherCampaigns
                .FirstOrDefaultAsync(x => x.Code == code);
        }

        public async Task<VoucherCampaign?> GetByIdAsync(Guid id)
        {
            return await _context.VoucherCampaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<VoucherCampaign?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.VoucherCampaigns
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
        {
            var query = _context.VoucherCampaigns.AsNoTracking().Where(x => x.Code == code);
            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> HasAnyRedemptionAsync(Guid campaignId)
        {
            return await _context.VoucherRedemptions
                .AsNoTracking()
                .AnyAsync(x => x.VoucherCampaignId == campaignId);
        }

        public async Task<(List<VoucherCampaign> Items, int TotalCount)> GetPagedAsync(VoucherCampaignFilterRequest filter)
        {
            IQueryable<VoucherCampaign> query = _context.VoucherCampaigns.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(x => x.Status == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();
                query = query.Where(x =>
                    EF.Functions.ILike(x.Code, $"%{keyword}%") ||
                    EF.Functions.ILike(x.Name, $"%{keyword}%"));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task AddWithoutSaveAsync(VoucherCampaign campaign)
        {
            _context.VoucherCampaigns.Add(campaign);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
