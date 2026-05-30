using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.PublicCoaches;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class PublicCoachRepository : IPublicCoachRepository
    {
        private readonly AppDbContext _context;
        public PublicCoachRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<CoachProfile?> GetByIdAsync(Guid coachId)
        {
            return await _context.CoachProfiles
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.CoachSports)
                    .ThenInclude(cs => cs.Sport)
                .Include(c => c.Media)
                .Include(c => c.TrainingPackages)
                    .ThenInclude(tp => tp.Sport)
                .FirstOrDefaultAsync(c => c.UserId == coachId && c.User.Status == "active");
        }

        public async Task<(List<CoachProfile> Items, int TotalCount)> GetPagedAsync(PublicCoachFilterRequest filter)
        {
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            
            var query = _context.CoachProfiles
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.CoachSports)
                    .ThenInclude(cs => cs.Sport)
                .Include(c => c.Media)
                .Include(c => c.TrainingPackages)
                .Where(c => c.User.Status == "active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Keyword))
            {
                var keyword = filter.Keyword.ToLower();
                query = query.Where(x =>
                    x.User.FullName.ToLower().Contains(keyword) ||
                    (x.Headline != null && x.Headline.ToLower().Contains(keyword)) ||
                    (x.Bio != null && x.Bio.ToLower().Contains(keyword)) ||
                    (x.Specialties != null && x.Specialties.ToLower().Contains(keyword)));
            }

            if(filter.SportId.HasValue)
            {
                query = query.Where(x => x.CoachSports.Any(cs => cs.SportId == filter.SportId.Value));
            }

            if (!string.IsNullOrEmpty(filter.City))
            {
                var city = filter.City.ToLower();
                query = query.Where(x => x.TeachingCity != null && x.TeachingCity.ToLower().Contains(city));
            }

            if (!string.IsNullOrEmpty(filter.District))
            {
                var district = filter.District.ToLower();
                query = query.Where(x => x.TeachingDistrict != null && x.TeachingDistrict.ToLower().Contains(district));
            }

            if (filter.IsOnlineAvailable.HasValue)
            {
                query = query.Where(x => x.IsOnlineAvailable == filter.IsOnlineAvailable.Value);
            }
            if (filter.IsOfflineAvailable.HasValue)
            {
                query = query.Where(x => x.IsOfflineAvailable == filter.IsOfflineAvailable.Value);
            }
            if (filter.MinRating.HasValue)
            {
                query = query.Where(x => x.Rating >= filter.MinRating.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.TotalReviews)
                .ThenByDescending(x => x.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, totalCount);
        }
    }
}
