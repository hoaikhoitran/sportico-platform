using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IPlatformSettingRepository
    {
        /// <summary>
        /// Current fractional commission rate (0..1) for NEW booking snapshots. Returns 0m when the
        /// singleton row is unexpectedly absent — a safe read that never writes during purchase.
        /// </summary>
        Task<decimal> GetCommissionRateAsync();

        /// <summary>
        /// Loads the singleton settings row without tracking, creating the 0% default first if it is
        /// unexpectedly absent (race-safe via the fixed singleton primary key).
        /// </summary>
        Task<PlatformSetting> GetOrCreateAsync();

        /// <summary>Tracked variant of <see cref="GetOrCreateAsync"/> for the admin update path.</summary>
        Task<PlatformSetting> GetOrCreateForUpdateAsync();

        Task SaveChangesAsync();
    }
}
