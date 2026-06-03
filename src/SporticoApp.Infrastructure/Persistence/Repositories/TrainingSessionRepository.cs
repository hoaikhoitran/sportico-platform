using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.TrainingSessions;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class TrainingSessionRepository : ITrainingSessionRepository
    {
        private readonly AppDbContext _context;

        public TrainingSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TrainingSession?> GetByIdAsync(Guid id)
        {
            return await _context.TrainingSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<TrainingSession?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.TrainingSessions
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(List<TrainingSession> Items, int TotalCount)> GetByBookingPagedAsync(
            Guid bookingId,
            TrainingSessionFilterRequest filter)
        {
            IQueryable<TrainingSession> query = _context.TrainingSessions
                .AsNoTracking()
                .Where(x => x.BookingId == bookingId);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.StartTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<int> CountByBookingAsync(Guid bookingId, List<string> statuses)
        {
            return await _context.TrainingSessions
                .AsNoTracking()
                .Where(x => x.BookingId == bookingId && statuses.Contains(x.Status))
                .CountAsync();
        }

        public async Task<int> CountActiveByAvailabilitySlotIdAsync(
            Guid slotId,
            IEnumerable<string> statuses,
            Guid? excludeSessionId = null)
        {
            var statusList = statuses.ToList();

            var query = _context.TrainingSessions
                .AsNoTracking()
                .Where(x => x.AvailabilitySlotId == slotId && statusList.Contains(x.Status));

            if (excludeSessionId.HasValue)
            {
                query = query.Where(x => x.Id != excludeSessionId.Value);
            }

            return await query.CountAsync();
        }

        public async Task<IReadOnlyDictionary<Guid, int>> CountActiveByAvailabilitySlotIdsAsync(
            IReadOnlyCollection<Guid> slotIds,
            IEnumerable<string> statuses)
        {
            if (slotIds.Count == 0)
            {
                return new Dictionary<Guid, int>();
            }

            var statusList = statuses.ToList();

            var grouped = await _context.TrainingSessions
                .AsNoTracking()
                .Where(x => x.AvailabilitySlotId != null
                            && slotIds.Contains(x.AvailabilitySlotId.Value)
                            && statusList.Contains(x.Status))
                .GroupBy(x => x.AvailabilitySlotId!.Value)
                .Select(g => new { SlotId = g.Key, Count = g.Count() })
                .ToListAsync();

            return grouped.ToDictionary(x => x.SlotId, x => x.Count);
        }

        public async Task<bool> HasOverlapAsync(
            Guid userId,
            DateTime startTime,
            DateTime endTime,
            List<string> activeStatuses)
        {
            return await _context.TrainingSessions
                .AsNoTracking()
                .Where(x => x.CoachId == userId || x.LearnerId == userId)
                .Where(x => activeStatuses.Contains(x.Status))
                .AnyAsync(x => startTime < x.EndTime && endTime > x.StartTime);
        }

        public async Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByLearnerAsync(
            Guid learnerId,
            TrainingSessionFilterRequest filter)
        {
            IQueryable<TrainingSession> query = _context.TrainingSessions
                .AsNoTracking()
                .Where(x => x.LearnerId == learnerId);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.StartTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<TrainingSession> Items, int TotalCount)> GetPagedByCoachAsync(
            Guid coachId,
            TrainingSessionFilterRequest filter)
        {
            IQueryable<TrainingSession> query = _context.TrainingSessions
                .AsNoTracking()
                .Where(x => x.CoachId == coachId);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.StartTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(TrainingSession session)
        {
            _context.TrainingSessions.Add(session);
            try
            {
                // This single SaveChanges persists the new session AND the slot mutation (status +
                // Version bump) tracked by the same DbContext, atomically.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // The availability slot's optimistic concurrency token (Version) changed: another
                // learner booked a seat on the same slot concurrently. Surface as a clean 409 so the
                // loser retries against the up-to-date seat count.
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "Availability slot was just updated by another booking. Please try again.");
            }
        }

        public Task AddWithoutSaveAsync(TrainingSession session)
        {
            _context.TrainingSessions.Add(session);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // A tracked availability slot's Version token changed under us (e.g. a concurrent
                // booking while this cancellation tried to re-open the slot). Surface as a retryable
                // 409 rather than a 500.
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "The availability slot was updated concurrently. Please try again.");
            }
        }

        private static IQueryable<TrainingSession> ApplyFilter(
            IQueryable<TrainingSession> query,
            TrainingSessionFilterRequest filter)
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
