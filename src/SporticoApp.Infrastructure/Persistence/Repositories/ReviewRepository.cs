using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Review?> GetByIdAsync(Guid id)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(x => x.learner)
                .Include(x => x.Coach).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Review?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.Reviews
                .Include(x => x.learner)
                .Include(x => x.Coach).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Review?> GetByCoachAndLearnerAsync(Guid coachId, Guid learnerId)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(x => x.learner)
                .Include(x => x.Coach).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(x => x.CoachId == coachId && x.learner_id == learnerId);
        }

        public async Task<Review?> GetByCoachAndLearnerForUpdateAsync(Guid coachId, Guid learnerId)
        {
            return await _context.Reviews
                .Include(x => x.learner)
                .Include(x => x.Coach).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(x => x.CoachId == coachId && x.learner_id == learnerId);
        }

        public async Task<(List<Review> Items, int TotalCount)> GetPagedByCoachAsync(
            Guid coachId,
            ReviewFilterRequest filter)
        {
            IQueryable<Review> query = _context.Reviews
                .AsNoTracking()
                .Include(x => x.learner)
                .Include(x => x.Coach).ThenInclude(c => c.User)
                .Where(x => x.CoachId == coachId && x.Status == ReviewStatuses.Active);

            if (filter.Rating is { } rating)
            {
                query = query.Where(x => x.Rating == rating);
            }

            var totalCount = await query.CountAsync();

            var sort = filter.SortBy?.Trim().ToLowerInvariant();
            query = sort switch
            {
                "highest" => query.OrderByDescending(x => x.Rating).ThenByDescending(x => x.CreatedAt),
                "lowest" => query.OrderBy(x => x.Rating).ThenByDescending(x => x.CreatedAt),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<CoachRatingStats> GetRatingStatsByCoachAsync(Guid coachId)
        {
            var rows = await _context.Reviews
                .AsNoTracking()
                .Where(x => x.CoachId == coachId && x.Status == ReviewStatuses.Active)
                .GroupBy(x => x.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();

            var stats = new CoachRatingStats();
            var weightedSum = 0;

            foreach (var row in rows)
            {
                stats.TotalReviews += row.Count;
                weightedSum += row.Rating * row.Count;

                switch (row.Rating)
                {
                    case 1: stats.OneStar = row.Count; break;
                    case 2: stats.TwoStar = row.Count; break;
                    case 3: stats.ThreeStar = row.Count; break;
                    case 4: stats.FourStar = row.Count; break;
                    case 5: stats.FiveStar = row.Count; break;
                }
            }

            stats.AverageRating = stats.TotalReviews > 0
                ? Math.Round((decimal)weightedSum / stats.TotalReviews, 2)
                : 0m;

            return stats;
        }

        public async Task<bool> HasSuccessfulBookingForReviewAsync(Guid learnerId, Guid coachId, Guid? bookingId)
        {
            var query = SuccessfulBookings(learnerId, coachId);

            if (bookingId.HasValue)
            {
                query = query.Where(b => b.Id == bookingId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> HasNonExpiredSuccessfulBookingAsync(Guid learnerId, Guid coachId)
        {
            var now = DateTime.UtcNow;
            return await SuccessfulBookings(learnerId, coachId)
                .Where(b => b.ExpiresAt == null || b.ExpiresAt >= now)
                .AnyAsync();
        }

        public async Task RecalculateCoachRatingAsync(Guid coachId)
        {
            var stats = await GetRatingStatsByCoachAsync(coachId);

            var coach = await _context.CoachProfiles
                .FirstOrDefaultAsync(c => c.UserId == coachId);

            if (coach == null)
            {
                return;
            }

            coach.Rating = stats.AverageRating;
            coach.TotalReviews = stats.TotalReviews;
            coach.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // A "successful" booking: belongs to the learner+coach, is active or completed, and is paid.
        private IQueryable<Booking> SuccessfulBookings(Guid learnerId, Guid coachId)
        {
            return _context.Bookings
                .AsNoTracking()
                .Where(b => b.LearnerId == learnerId
                    && b.CoachId == coachId
                    && b.PaidAt != null
                    && (b.Status == BookingStatuses.Active || b.Status == BookingStatuses.Completed));
        }
    }
}
