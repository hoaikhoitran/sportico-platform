using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task AddWithoutSaveAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task<Notification?> GetByIdForUpdateAsync(Guid userId, Guid notificationId)
        {
            return _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
        }

        public Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, NotificationFilterRequest filter)
        {
            IQueryable<Notification> query = _context.Notifications
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            if (filter.IsRead.HasValue)
            {
                query = query.Where(x => x.IsRead == filter.IsRead.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                var normalized = filter.Type.Trim().ToLowerInvariant();
                query = query.Where(x => x.Type.ToLower() == normalized);
            }

            return GetPagedAsync(query, filter.PageNumber, filter.PageSize);
        }

        public Task<int> GetUnreadCountAsync(Guid userId)
        {
            return _context.Notifications
                .AsNoTracking()
                .CountAsync(x => x.UserId == userId && !x.IsRead);
        }

        public Task<List<Notification>> GetUnreadForUpdateAsync(Guid userId)
        {
            return _context.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ToListAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        private static async Task<(List<Notification> Items, int TotalCount)> GetPagedAsync(
            IQueryable<Notification> query,
            int pageNumber,
            int pageSize)
        {
            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
