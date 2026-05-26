using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            throw new NotImplementedException();
        }

        public Task<Notification?> GetByIdForUpdateAsync(Guid userId, Guid notificationId)
        {
            throw new NotImplementedException();
        }

        public Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, NotificationFilterRequest filter)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetUnreadCountAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Notification>> GetUnreadForUpdateAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
