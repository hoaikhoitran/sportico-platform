using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class PlatformSettingRepository : IPlatformSettingRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PlatformSettingRepository> _logger;

        public PlatformSettingRepository(
            AppDbContext context,
            ILogger<PlatformSettingRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>True when the exception means the platform_settings table has not been migrated (42P01).</summary>
        private static bool IsMissingTable(PostgresException ex)
            => ex.SqlState == PostgresErrorCodes.UndefinedTable;

        public async Task<decimal> GetCommissionRateAsync()
        {
            // Pure read on the purchase path: never writes. The migration seeds the singleton row;
            // if it is unexpectedly absent, fall back to the safe 0% default.
            try
            {
                var rate = await _context.PlatformSettings
                    .AsNoTracking()
                    .Where(x => x.Id == PlatformSetting.SingletonId)
                    .Select(x => (decimal?)x.CommissionRate)
                    .FirstOrDefaultAsync();

                return rate ?? 0m;
            }
            catch (PostgresException ex) when (IsMissingTable(ex))
            {
                // The platform_settings table itself is missing — migration
                // AddConfigurablePlatformCommission was not applied to this database. A purchase
                // must NEVER fail because of that ops gap: fall back to the same 0% default the
                // migration would have seeded, and log loudly so the gap gets fixed.
                _logger.LogError(
                    ex,
                    "platform_settings table does not exist (migration AddConfigurablePlatformCommission " +
                    "not applied to this database). Falling back to the default 0% commission for this " +
                    "booking snapshot. Run 'dotnet ef database update' against this database.");

                return 0m;
            }
        }

        public async Task<PlatformSetting> GetOrCreateAsync()
        {
            try
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
            catch (PostgresException ex) when (IsMissingTable(ex))
            {
                throw MissingTableFailure(ex);
            }
        }

        public async Task<PlatformSetting> GetOrCreateForUpdateAsync()
        {
            try
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
            catch (PostgresException ex) when (IsMissingTable(ex))
            {
                throw MissingTableFailure(ex);
            }
        }

        /// <summary>
        /// Admin settings endpoints cannot self-heal a missing TABLE (only a missing row), so they
        /// surface a clean, actionable error in the standard envelope instead of a raw 500.
        /// </summary>
        private FailureException MissingTableFailure(PostgresException ex)
        {
            _logger.LogError(
                ex,
                "platform_settings table does not exist (migration AddConfigurablePlatformCommission " +
                "not applied to this database). Run 'dotnet ef database update' against this database.");

            return new FailureException(
                ErrorCodes.PlatformSettingsUnavailable,
                "Platform settings storage has not been migrated yet. Apply the pending database migrations and try again.");
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
            catch (DbUpdateException ex)
            {
                // Detach the failed insert so the context stays usable, then decide why it failed.
                _context.Entry(setting).State = EntityState.Detached;

                if (ex.InnerException is PostgresException pg && IsMissingTable(pg))
                {
                    // No table to insert into — this is the unmigrated-database case, not a race.
                    throw MissingTableFailure(pg);
                }

                // Otherwise we lost the create race (or the seed row appeared): the row now
                // exists and the caller re-reads the winner's row.
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
