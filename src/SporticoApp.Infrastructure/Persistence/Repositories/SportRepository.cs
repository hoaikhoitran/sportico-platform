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
    public class SportRepository : ISportRepository
    {
        private readonly AppDbContext _context;

        public SportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<int>> GetActiveSportIdsAsync(List<int> sportIds)
        {
            if (sportIds.Count == 0)
            {
                return new List<int>();
            }

            var distinctIds = sportIds
                .Distinct()
                .ToList();

            return await _context.Sports
                .AsNoTracking()
                .Where(x => distinctIds.Contains(x.Id) && x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            var normalizedName = name.ToLowerInvariant();

            return await _context.Sports
                .AsNoTracking()
                .AnyAsync(x => x.Name.ToLower() == normalizedName);
        }

        public async Task<bool> ExistsBySlugAsync(string slug)
        {
            return await _context.Sports
                .AsNoTracking()
                .AnyAsync(x => x.Slug == slug);
        }

        public async Task<Sport?> GetByIdAsync(int id)
        {
            return await _context.Sports
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(Sport sport)
        {
            _context.Sports.Add(sport);
            await _context.SaveChangesAsync();
        }
    }
}
