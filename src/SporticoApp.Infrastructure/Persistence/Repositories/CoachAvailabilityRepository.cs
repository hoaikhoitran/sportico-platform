using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Availability;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CoachAvailabilityRepository : ICoachAvailabilityRepository
    {
        private readonly AppDbContext _context;

        public CoachAvailabilityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CoachAvailabilitySlot?> GetByIdAsync(Guid id)
        {
            return await _context.CoachAvailabilitySlots
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<CoachAvailabilitySlot?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.CoachAvailabilitySlots
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetByCoachPagedAsync(
            Guid coachId,
            CoachAvailabilitySlotFilterRequest filter)
        {
            IQueryable<CoachAvailabilitySlot> query = _context.CoachAvailabilitySlots
                .AsNoTracking()
                .Where(x => x.CoachId == coachId);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.StartTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<CoachAvailabilitySlot> Items, int TotalCount)> GetAvailableByCoachPagedAsync(
            Guid coachId,
            CoachAvailabilitySlotFilterRequest filter)
        {
            IQueryable<CoachAvailabilitySlot> query = _context.CoachAvailabilitySlots
                .AsNoTracking()
                .Where(x => x.CoachId == coachId
                            && x.Status == CoachAvailabilitySlotStatuses.Available
                            && x.StartTime > DateTime.UtcNow);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.StartTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> HasOverlapAsync(
            Guid coachId,
            DateTime startTime,
            DateTime endTime,
            Guid? excludeSlotId = null)
        {
            var query = _context.CoachAvailabilitySlots
                .AsNoTracking()
                .Where(x => x.CoachId == coachId
                            && x.Status != CoachAvailabilitySlotStatuses.Cancelled
                            && startTime < x.EndTime && endTime > x.StartTime);

            if (excludeSlotId.HasValue)
            {
                query = query.Where(x => x.Id != excludeSlotId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task AddAsync(CoachAvailabilitySlot slot)
        {
            _context.CoachAvailabilitySlots.Add(slot);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // The slot's Version token changed under us (e.g. a learner booked a seat while the
                // coach was cancelling the slot). Surface as a retryable 409 rather than a 500.
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "The availability slot was updated concurrently. Please try again.");
            }
        }

        private static IQueryable<CoachAvailabilitySlot> ApplyFilter(
            IQueryable<CoachAvailabilitySlot> query,
            CoachAvailabilitySlotFilterRequest filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalized = filter.Status.Trim().ToLowerInvariant();
                query = query.Where(x => x.Status.ToLower() == normalized);
            }

            if (filter.StartFrom.HasValue)
            {
                query = query.Where(x => x.StartTime >= filter.StartFrom.Value);
            }

            if (filter.StartTo.HasValue)
            {
                query = query.Where(x => x.StartTime <= filter.StartTo.Value);
            }

            return query;
        }
    }
}
