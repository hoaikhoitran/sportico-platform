using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(
           Guid userId,
           NotificationFilterRequest filter);

        Task<int> GetUnreadCountAsync(Guid userId);

        Task<Notification?> GetByIdForUpdateAsync(
            Guid userId,
            Guid notificationId);

        Task<List<Notification>> GetUnreadForUpdateAsync(Guid userId);

        Task AddWithoutSaveAsync(Notification notification);

        Task SaveChangesAsync();
    }
}
