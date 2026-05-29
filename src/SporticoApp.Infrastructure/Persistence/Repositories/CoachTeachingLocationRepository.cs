using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CoachTeachingLocationRepository : ICoachTeachingLocationRepository
    {
        private readonly AppDbContext _context;

        public CoachTeachingLocationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CoachTeachingLocation>> GetByCoachIdAsync(Guid coachId)
        {
            return await _context.CoachTeachingLocations
                .AsNoTracking()
                .Where(x => x.CoachId == coachId)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<CoachTeachingLocation?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.CoachTeachingLocations
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(CoachTeachingLocation location)
        {
            _context.CoachTeachingLocations.Add(location);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CoachTeachingLocation location)
        {
            _context.CoachTeachingLocations.Update(location);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(CoachTeachingLocation location)
        {
            _context.CoachTeachingLocations.Remove(location);
            await _context.SaveChangesAsync();
        }

        public async Task ClearDefaultsAsync(Guid coachId, Guid exceptId)
        {
            await _context.CoachTeachingLocations
                .Where(x => x.CoachId == coachId
                    && x.Id != exceptId
                    && x.IsDefault)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsDefault, false)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
        }
    }
}
