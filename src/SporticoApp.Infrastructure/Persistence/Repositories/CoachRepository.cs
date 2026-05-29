using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CoachRepository : ICoachRepository
    {
        private readonly AppDbContext _context;

        public CoachRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId)
        {
            return await _context.CoachProfiles
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId);
        }

        public async Task<CoachProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.CoachProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<CoachProfile?> GetByUserIdWithDetailsAsync(Guid userId)
        {
            return await _context.CoachProfiles
                .AsNoTracking()
                .Include(x => x.Media)
                .Include(x => x.TeachingLocations)
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<CoachProfile?> GetByUserIdForUpdateAsync(Guid userId)
        {
            return await _context.CoachProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task CreateCoachProfileAsync(
            CoachProfile coachProfile,
            int coachRoleId,
            List<int> sportIds)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var alreadyHasCoachRole = await _context.UserRoles
                .AnyAsync(x =>
                    x.UserId == coachProfile.UserId &&
                    x.RoleId == coachRoleId);

            if (!alreadyHasCoachRole)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = coachProfile.UserId,
                    RoleId = coachRoleId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.CoachProfiles.Add(coachProfile);

            if (sportIds.Count > 0)
            {
                var coachSports = sportIds
                    .Distinct()
                    .Select(sportId => new CoachSport
                    {
                        CoachId = coachProfile.UserId,
                        SportId = sportId,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                _context.CoachSports.AddRange(coachSports);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
    }
}
