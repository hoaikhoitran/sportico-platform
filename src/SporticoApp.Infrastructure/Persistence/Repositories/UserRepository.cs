using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public Task AddWithoutSaveAsync(User user)
        {
            _context.Users.Add(user);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByEmailWithRolesAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User?> GetByVerificationTokenAsync(
            string token)
        {
            return await _context.Users
                .FirstOrDefaultAsync(
                    x => x.EmailVerificationToken == token);
        }

        public async Task<User?> GetByPasswordResetTokenAsync(
            string token)
        {
            return await _context.Users
                .FirstOrDefaultAsync(
                    x => x.PasswordResetToken == token);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetByIdWithProfilesAndRolesAsync(Guid id)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.CoachProfile)
                .Include(u => u.LearnerProfile)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        // Tracked (no AsNoTracking): the token issuer writes RefreshToken/RefreshTokenExpiresAt
        // onto the returned entity and expects SaveChanges to pick it up.
        public async Task<User?> GetByIdWithRolesAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        // ── Admin user management ────────────────────────────────────────────

        public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedForAdminAsync(
            AdminUserFilterRequest filter)
        {
            IQueryable<User> query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = $"%{filter.Search.Trim()}%";
                query = query.Where(u =>
                    EF.Functions.ILike(u.Email, pattern) ||
                    EF.Functions.ILike(u.FullName, pattern) ||
                    (u.Phone != null && EF.Functions.ILike(u.Phone, pattern)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                var role = filter.Role.Trim().ToLower();
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name.ToLower() == role));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var status = filter.Status.Trim().ToLower();
                query = query.Where(u => u.Status.ToLower() == status);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.CoachProfile)
                .Include(u => u.LearnerProfile)
                .OrderByDescending(u => u.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<User?> GetByIdForAdminUpdateAsync(Guid id)
        {
            // Tracked (no AsNoTracking) so role replacement and field updates persist.
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == email);
        }
    }
}
