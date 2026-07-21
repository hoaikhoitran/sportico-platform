using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class VisitorTrackingRepository : IVisitorTrackingRepository
    {
        private readonly AppDbContext _context;

        public VisitorTrackingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPriorSessionAsync(Guid visitorId)
        {
            return await _context.VisitorSessions
                .AsNoTracking()
                .AnyAsync(s => s.VisitorId == visitorId);
        }

        public async Task<VisitorSession?> GetOpenSessionForUpdateAsync(Guid visitorId, DateTime idleSinceUtc)
        {
            return await _context.VisitorSessions
                .Where(s => s.VisitorId == visitorId && s.LastSeenAt >= idleSinceUtc)
                .OrderByDescending(s => s.LastSeenAt)
                .FirstOrDefaultAsync();
        }

        public Task AddSessionWithoutSaveAsync(VisitorSession session)
        {
            _context.VisitorSessions.Add(session);
            return Task.CompletedTask;
        }

        public Task AddApiRequestMetricWithoutSaveAsync(ApiRequestMetric metric)
        {
            _context.ApiRequestMetrics.Add(metric);
            return Task.CompletedTask;
        }

        public Task AddPageViewWithoutSaveAsync(PageView pageView)
        {
            _context.PageViews.Add(pageView);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
