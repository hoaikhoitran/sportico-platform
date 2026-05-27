using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class TrainingPackageRepository : ITrainingPackageRepository
    {
        private readonly AppDbContext _context;

        public TrainingPackageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TrainingPackage?> GetByIdAsync(Guid id)
        {
            return await _context.TrainingPackages
                .AsNoTracking()
                .Include(x => x.Sport)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<TrainingPackage?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.TrainingPackages
                .Include(x => x.Sport)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<TrainingPackage?> GetOwnedByIdAsync(Guid coachId, Guid id)
        {
            return await _context.TrainingPackages
                .AsNoTracking()
                .Include(x => x.Sport)
                .FirstOrDefaultAsync(x => x.Id == id && x.CoachId == coachId);
        }

        public async Task<TrainingPackage?> GetOwnedByIdForUpdateAsync(Guid coachId, Guid id)
        {
            return await _context.TrainingPackages
                .Include(x => x.Sport)
                .FirstOrDefaultAsync(x => x.Id == id && x.CoachId == coachId);
        }

        public async Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedAsync(
            TrainingPackageFilterRequest filter)
        {
            IQueryable<TrainingPackage> query = _context.TrainingPackages
                .AsNoTracking()
                .Include(x => x.Sport);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(TrainingPackage trainingPackage)
        {
            _context.TrainingPackages.Add(trainingPackage);
            await _context.SaveChangesAsync();
        }

        public Task AddWithoutSaveAsync(TrainingPackage trainingPackage)
        {
            _context.TrainingPackages.Add(trainingPackage);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private static IQueryable<TrainingPackage> ApplyFilter(
            IQueryable<TrainingPackage> query,
            TrainingPackageFilterRequest filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var normalized = filter.Keyword.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(normalized) ||
                    (x.Description != null && x.Description.ToLower().Contains(normalized)));
            }

            if (filter.SportId.HasValue)
            {
                query = query.Where(x => x.SportId == filter.SportId.Value);
            }

            if (filter.CoachId.HasValue)
            {
                query = query.Where(x => x.CoachId == filter.CoachId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalized = filter.Status.Trim().ToLowerInvariant();
                query = query.Where(x => x.Status.ToLower() == normalized);
            }

            return query;
        }
    }
}
