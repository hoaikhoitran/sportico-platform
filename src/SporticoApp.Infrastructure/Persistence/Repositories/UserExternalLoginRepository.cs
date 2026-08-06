using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class UserExternalLoginRepository : IUserExternalLoginRepository
    {
        private readonly AppDbContext _context;

        public UserExternalLoginRepository(AppDbContext context)
        {
            _context = context;
        }

        // Tracked on purpose: callers write LastLoginAt on the returned entity.
        public async Task<UserExternalLogin?> GetByProviderSubjectAsync(string provider, string providerSubject)
        {
            return await _context.UserExternalLogins
                .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderSubject == providerSubject);
        }

        public async Task<UserExternalLogin?> GetByUserAndProviderAsync(Guid userId, string provider)
        {
            return await _context.UserExternalLogins
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == provider);
        }

        public Task AddWithoutSaveAsync(UserExternalLogin externalLogin)
        {
            _context.UserExternalLogins.Add(externalLogin);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
