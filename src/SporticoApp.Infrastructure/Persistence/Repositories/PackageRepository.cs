using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class PackageRepository : IPackageRepository
    {
        private readonly AppDbContext _context;

        public PackageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            var normalized = name.Trim().ToLowerInvariant();

            return await _context.Packages
                .AsNoTracking()
                .AnyAsync(x => x.Name.ToLower() == normalized);
        }

        public async Task<bool> ExistsByNameExceptIdAsync(
            string name,
            int excludedId)
        {
            var normalized = name.Trim().ToLowerInvariant();

            return await _context.Packages
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id != excludedId &&
                    x.Name.ToLower() == normalized);
        }

        public async Task<Package?> GetByIdAsync(int id)
        {
            return await _context.Packages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Package?> GetForUpdateByIdAsync(int id)
        {
            return await _context.Packages
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Package?> GetActiveByIdAsync(int id)
        {
            return await _context.Packages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        }

        public async Task<(List<Package> Items, int TotalCount)> GetPagedAsync(
            string? keyword,
            bool? isActive,
            int pageNumber,
            int pageSize)
        {
            var query = _context.Packages.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalized = keyword.Trim().ToLowerInvariant();

                query = query.Where(x =>
                    x.Name.ToLower().Contains(normalized) ||
                    (x.Description != null &&
                     x.Description.ToLower().Contains(normalized)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(Package package)
        {
            _context.Packages.Add(package);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
