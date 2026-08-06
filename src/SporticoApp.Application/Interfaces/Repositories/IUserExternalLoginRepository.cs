using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IUserExternalLoginRepository
    {
        /// <summary>
        /// Looks up a link by the provider's stable subject. Tracked (not AsNoTracking) because the
        /// caller updates LastLoginAt on the returned row.
        /// </summary>
        Task<UserExternalLogin?> GetByProviderSubjectAsync(string provider, string providerSubject);

        Task<UserExternalLogin?> GetByUserAndProviderAsync(Guid userId, string provider);

        Task AddWithoutSaveAsync(UserExternalLogin externalLogin);

        Task SaveChangesAsync();
    }
}
