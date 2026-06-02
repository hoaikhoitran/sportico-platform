using Microsoft.EntityFrameworkCore;
using Npgsql;
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
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsActiveSlotUniqueViolation(ex))
            {
                // A concurrent request already attached an active session to this slot.
                // The filtered unique index uq_training_sessions_active_slot rejected the insert;
                // surface it as a clean 409 ScheduleConflict instead of a 500.
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "Availability slot is no longer available");
            }
        }

        private static bool IsActiveSlotUniqueViolation(DbUpdateException ex)
            => ex.InnerException is PostgresException pg
               && pg.SqlState == PostgresErrorCodes.UniqueViolation
               && pg.ConstraintName == "uq_training_sessions_active_slot";

        public Task AddWithoutSaveAsync(TrainingSession session)
        {
            _context.TrainingSessions.Add(session);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
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
