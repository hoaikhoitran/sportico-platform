using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class AuthExchangeCodeRepository : IAuthExchangeCodeRepository
    {
        private readonly AppDbContext _context;

        public AuthExchangeCodeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuthExchangeCode code)
        {
            _context.AuthExchangeCodes.Add(code);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Single conditional UPDATE: the WHERE clause requires the row to still be unused and
        /// unexpired, and PostgreSQL takes a row lock for the duration. Two concurrent requests with
        /// the same code therefore serialise, and only the first sees rowsAffected == 1 — the second
        /// finds used_at already set and matches nothing. This is why consuming a code cannot be a
        /// read-then-write in application code.
        /// </summary>
        public async Task<AuthExchangeCode?> ConsumeAsync(string codeHash, DateTime nowUtc)
        {
            var rowsAffected = await _context.AuthExchangeCodes
                .Where(x => x.CodeHash == codeHash && x.UsedAt == null && x.ExpiresAt > nowUtc)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAt, nowUtc));

            if (rowsAffected == 0)
            {
                return null;
            }

            return await _context.AuthExchangeCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CodeHash == codeHash);
        }

        public async Task<AuthExchangeCode?> FindAnyAsync(string codeHash)
        {
            return await _context.AuthExchangeCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CodeHash == codeHash);
        }

        /// <summary>
        /// Best-effort housekeeping invoked when a new code is issued — keeps the table small
        /// without a dedicated background worker. A failure here must never break a login.
        /// </summary>
        public async Task DeleteExpiredAsync(DateTime nowUtc)
        {
            try
            {
                var cutoff = nowUtc.AddMinutes(-30);
                await _context.AuthExchangeCodes
                    .Where(x => x.ExpiresAt < cutoff || (x.UsedAt != null && x.UsedAt < cutoff))
                    .ExecuteDeleteAsync();
            }
            catch
            {
                // Swallow: cleanup is opportunistic, the login must still succeed.
            }
        }
    }
}
