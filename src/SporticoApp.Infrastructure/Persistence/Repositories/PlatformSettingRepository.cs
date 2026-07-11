using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class PlatformSettingRepository : IPlatformSettingRepository
    {
        private readonly AppDbContext _context;

        public PlatformSettingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetCommissionRateAsync()
        {
            // Pure read on the purchase path: never writes. The migration seeds the singleton row;
            // if it is unexpectedly absent, fall back to the safe 0% default.
            var rate = await _context.PlatformSettings
                .AsNoTracking()
                .Where(x => x.Id == PlatformSetting.SingletonId)
                .Select(x => (decimal?)x.CommissionRate)
                .FirstOrDefaultAsync();

            return rate ?? 0m;
        }

        public async Task<PlatformSetting> GetOrCreateAsync()
        {
            var setting = await _context.PlatformSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == PlatformSetting.SingletonId);

            if (setting != null)
            {
                return setting;
            }

            await EnsureSingletonAsync();

            return await _context.PlatformSettings
                .AsNoTracking()
                .FirstAsync(x => x.Id == PlatformSetting.SingletonId);
        }

        public async Task<PlatformSetting> GetOrCreateForUpdateAsync()
        {
            var setting = await _context.PlatformSettings
                .FirstOrDefaultAsync(x => x.Id == PlatformSetting.SingletonId);

            if (setting != null)
            {
                return setting;
            }

            await EnsureSingletonAsync();

            return await _context.PlatformSettings
                .FirstAsync(x => x.Id == PlatformSetting.SingletonId);
        }

        /// <summary>
        /// Inserts the 0% default singleton row. Race-safe: a concurrent insert of the same fixed
        /// primary key makes this SaveChanges fail, in which case the row now exists and the caller
        /// simply re-reads it.
        /// </summary>
        private async Task EnsureSingletonAsync()
        {
            var now = DateTime.UtcNow;
            var setting = new PlatformSetting
            {
                Id = PlatformSetting.SingletonId,
                CommissionRate = 0m,
                CreatedAt = now,
                UpdatedAt = now,
                Version = 0
            };

            _context.PlatformSettings.Add(setting);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Lost the create race (or the seed row appeared) — detach the failed insert so the
                // context stays usable, then let the caller re-read the winner's row.
                _context.Entry(setting).State = EntityState.Detached;
            }
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // PlatformSetting.Version optimistic concurrency: two admins saved the settings at
                // the same time. Surface as a retryable 409 rather than a 500.
                throw new ConflictException(
                    ErrorCodes.ConcurrencyConflict,
                    "The platform settings were updated concurrently. Please try again.");
            }
        }
    }
}
