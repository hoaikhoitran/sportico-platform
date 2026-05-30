using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetByIdAsync(Guid id)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Include(x => x.TrainingPackage)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Booking?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.Bookings
                .Include(x => x.TrainingPackage)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Booking?> GetByIdWithTrainingPackageAsync(Guid id)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Include(x => x.TrainingPackage)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Booking?> GetByIdForLearnerAsync(Guid learnerId, Guid id)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Include(x => x.TrainingPackage)
                .FirstOrDefaultAsync(x => x.Id == id && x.LearnerId == learnerId);
        }

        public async Task<Booking?> GetByIdForCoachAsync(Guid coachId, Guid id)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Include(x => x.TrainingPackage)
                .FirstOrDefaultAsync(x => x.Id == id && x.CoachId == coachId);
        }

        public async Task<Booking?> GetByIdForLearnerForUpdateAsync(Guid learnerId, Guid id)
        {
            return await _context.Bookings
                .Include(x => x.TrainingPackage)
                .FirstOrDefaultAsync(x => x.Id == id && x.LearnerId == learnerId);
        }

        public async Task<Booking?> GetByIdForCoachForUpdateAsync(Guid coachId, Guid id)
        {
            return await _context.Bookings
                .Include(x => x.TrainingPackage)
                .FirstOrDefaultAsync(x => x.Id == id && x.CoachId == coachId);
        }

        public async Task<(List<Booking> Items, int TotalCount)> GetPagedByLearnerAsync(
            Guid learnerId,
            BookingFilterRequest filter)
        {
            IQueryable<Booking> query = _context.Bookings
                .AsNoTracking()
                .Include(x => x.TrainingPackage)
                .Where(x => x.LearnerId == learnerId);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Booking> Items, int TotalCount)> GetPagedByCoachAsync(
            Guid coachId,
            BookingFilterRequest filter)
        {
            IQueryable<Booking> query = _context.Bookings
                .AsNoTracking()
                .Include(x => x.TrainingPackage)
                .Where(x => x.CoachId == coachId);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Booking> Items, int TotalCount)> GetPagedAsync(
            BookingFilterRequest filter)
        {
            IQueryable<Booking> query = _context.Bookings
                .AsNoTracking()
                .Include(x => x.TrainingPackage);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Booking?> GetActiveOrCompletedBetweenUsersAsync(
            Guid learnerId,
            Guid coachId)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Where(x => x.LearnerId == learnerId && x.CoachId == coachId)
                .Where(x => x.Status == BookingStatuses.Active || x.Status == BookingStatuses.Completed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(Booking booking)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
        }

        public Task AddWithoutSaveAsync(Booking booking)
        {
            _context.Bookings.Add(booking);
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
                // CoachWallet.Version optimistic concurrency: two concurrent session completions
                // for the same coach collide here. Surface as 409 so the caller can retry.
                throw new ConflictException(
                    ErrorCodes.InsufficientWalletBalance,
                    "A concurrent wallet update was detected. Please try again.");
            }
        }

        private static IQueryable<Booking> ApplyFilter(
            IQueryable<Booking> query,
            BookingFilterRequest filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalized = filter.Status.Trim().ToLowerInvariant();
                query = query.Where(x => x.Status.ToLower() == normalized);
            }

            return query;
        }
    }
}
