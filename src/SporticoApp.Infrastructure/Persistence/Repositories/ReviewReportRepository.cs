using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class ReviewReportRepository : IReviewReportRepository
    {
        private readonly AppDbContext _context;

        public ReviewReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Report?> GetByIdAsync(Guid id)
        {
            return await _context.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.TargetType == ReportTargetTypes.Review);
        }

        public async Task<Report?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.Reports
                .FirstOrDefaultAsync(x => x.Id == id && x.TargetType == ReportTargetTypes.Review);
        }

        public async Task<Report?> GetOpenReportAsync(Guid reviewId, Guid reporterId)
        {
            return await _context.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TargetType == ReportTargetTypes.Review &&
                    x.TargetId == reviewId &&
                    x.reporter_id == reporterId &&
                    (x.Status == ReportStatuses.Pending || x.Status == ReportStatuses.Reviewing));
        }

        public async Task<(List<Report> Items, int TotalCount)> GetPagedReviewReportsAsync(
            ReviewReportFilterRequest filter)
        {
            IQueryable<Report> query = _context.Reports
                .AsNoTracking()
                .Where(x => x.TargetType == ReportTargetTypes.Review);

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalized = filter.Status.Trim().ToLowerInvariant();
                query = query.Where(x => x.Status == normalized);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(Report report)
        {
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
