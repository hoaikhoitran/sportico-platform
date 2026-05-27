using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class ProgressCheckInRepository : IProgressCheckInRepository
    {
        private readonly AppDbContext _context;

        public ProgressCheckInRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProgressCheckIn?> GetByIdAsync(Guid id)
        {
            return await _context.ProgressCheckIns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ProgressCheckIn?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.ProgressCheckIns
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(List<ProgressCheckIn> Items, int TotalCount)> GetByBookingPagedAsync(
            Guid bookingId,
            int pageNumber,
            int pageSize)
        {
            var query = _context.ProgressCheckIns
                .AsNoTracking()
                .Where(x => x.BookingId == bookingId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CheckInDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(ProgressCheckIn checkIn)
        {
            _context.ProgressCheckIns.Add(checkIn);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
