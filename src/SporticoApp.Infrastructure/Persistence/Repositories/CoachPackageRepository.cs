using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CoachPackageRepository : ICoachPackageRepository
    {
        private readonly AppDbContext _context;

        public CoachPackageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CoachPackage?> GetCurrentByCoachIdAsync(Guid coachId)
        {
            return await _context.CoachPackages
                .AsNoTracking()
                .Include(x => x.Package)
                .Where(x =>
                    x.CoachId == coachId &&
                    (x.Status == CoachPackageStatuses.Active ||
                     x.Status == CoachPackageStatuses.Pending))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<CoachPackage?> GetCurrentForUpdateAsync(Guid coachId)
        {
            return await _context.CoachPackages
                .Where(x =>
                    x.CoachId == coachId &&
                    (x.Status == CoachPackageStatuses.Active ||
                     x.Status == CoachPackageStatuses.Pending))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<(List<CoachPackage> Items, int TotalCount)> GetHistoryAsync(
            Guid coachId,
            int pageNumber,
            int pageSize)
        {
            var query = _context.CoachPackages
                .AsNoTracking()
                .Include(x => x.Package)
                .Where(x => x.CoachId == coachId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(CoachPackage coachPackage)
        {
            _context.CoachPackages.Add(coachPackage);
            await _context.SaveChangesAsync();
        }

        public Task AddWithoutSaveAsync(CoachPackage coachPackage)
        {
            _context.CoachPackages.Add(coachPackage);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}