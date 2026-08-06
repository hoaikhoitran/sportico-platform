using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IAuthExchangeCodeRepository
    {
        Task AddAsync(AuthExchangeCode code);

        /// <summary>
        /// Atomically marks the (unused, unexpired) code identified by <paramref name="codeHash"/>
        /// as used and returns its row. Implementations MUST perform the "check unused + set
        /// UsedAt" step as a single conditional database write, so that two concurrent requests
        /// with the same code can never both succeed. Returns null when no row matched.
        /// </summary>
        Task<AuthExchangeCode?> ConsumeAsync(string codeHash, DateTime nowUtc);

        /// <summary>
        /// Returns the row for a hash regardless of used/expired state — used only to tell
        /// "expired" and "already used" apart for a precise error code after ConsumeAsync misses.
        /// </summary>
        Task<AuthExchangeCode?> FindAnyAsync(string codeHash);

        /// <summary>Opportunistic cleanup of expired/consumed rows. Best-effort; never throws.</summary>
        Task DeleteExpiredAsync(DateTime nowUtc);
    }
}
