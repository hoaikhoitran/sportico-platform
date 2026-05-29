using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CoachProfileMediaRepository : ICoachProfileMediaRepository
    {
        private readonly AppDbContext _context;

        public CoachProfileMediaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CoachProfileMedia>> GetByCoachIdAsync(Guid coachId)
        {
            return await _context.CoachProfileMedia
                .AsNoTracking()
                .Where(x => x.CoachId == coachId)
                .OrderBy(x => x.OrderIndex)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<CoachProfileMedia?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.CoachProfileMedia
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(CoachProfileMedia media)
        {
            _context.CoachProfileMedia.Add(media);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CoachProfileMedia media)
        {
            _context.CoachProfileMedia.Update(media);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(CoachProfileMedia media)
        {
            _context.CoachProfileMedia.Remove(media);
            await _context.SaveChangesAsync();
        }
    }
}
