using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CommunityReportRepository : ICommunityReportRepository
    {
        private static readonly string[] CommunityTargetTypes =
        {
            ReportTargetTypes.CommunityPost, ReportTargetTypes.CommunityComment, ReportTargetTypes.ChatMessage
        };

        private readonly AppDbContext _context;

        public CommunityReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<Report?> GetByIdForUpdateAsync(Guid id)
        {
            return _context.Reports.FirstOrDefaultAsync(x => x.Id == id && CommunityTargetTypes.Contains(x.TargetType));
        }

        public Task<Report?> GetOpenReportAsync(string targetType, Guid targetId, Guid reporterId)
        {
            return _context.Reports.FirstOrDefaultAsync(x =>
                x.TargetType == targetType &&
                x.TargetId == targetId &&
                x.reporter_id == reporterId &&
                (x.Status == ReportStatuses.Pending || x.Status == ReportStatuses.Reviewing));
        }

        public Task<int> CountOpenByTargetAsync(string targetType, Guid targetId)
        {
            return _context.Reports.CountAsync(x =>
                x.TargetType == targetType &&
                x.TargetId == targetId &&
                (x.Status == ReportStatuses.Pending || x.Status == ReportStatuses.Reviewing));
        }

        public async Task<(List<Report> Items, int TotalCount)> GetPagedAsync(AdminReportFilterRequest filter)
        {
            IQueryable<Report> query = _context.Reports
                .AsNoTracking()
                .Where(x => CommunityTargetTypes.Contains(x.TargetType));

            if (!string.IsNullOrWhiteSpace(filter.TargetType))
            {
                query = query.Where(x => x.TargetType == filter.TargetType);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(x => x.Status == filter.Status);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task AddWithoutSaveAsync(Report report)
        {
            _context.Reports.Add(report);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
