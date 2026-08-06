using SporticoApp.Application.DTOs.Users;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByEmailWithRolesAsync(string email);
        Task AddAsync(User user);

        Task AddWithoutSaveAsync(User user);
        Task SaveChangesAsync();

        Task<User?> GetByVerificationTokenAsync(string token);
        Task<User?> GetByPasswordResetTokenAsync(string token);
        Task UpdateAsync(User user);
        Task<User?> GetByIdAsync(Guid id);

        Task<User?> GetByIdWithProfilesAndRolesAsync(Guid id);
        Task<User?> GetByIdForUpdateAsync(Guid id);

        /// <summary>
        /// Tracked user with UserRoles + Role loaded. Needed by every login path because the JWT
        /// carries role claims — issuing a token from a user without roles would silently drop them.
        /// </summary>
        Task<User?> GetByIdWithRolesAsync(Guid id);

        // ── Admin user management ────────────────────────────────────────────
        /// <summary>Paged admin list with search (email/name/phone), role and status filters.</summary>
        Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedForAdminAsync(AdminUserFilterRequest filter);

        /// <summary>Tracked user including UserRoles, for admin update (role replacement).</summary>
        Task<User?> GetByIdForAdminUpdateAsync(Guid id);

        Task<bool> ExistsByEmailAsync(string email);
    }
}
