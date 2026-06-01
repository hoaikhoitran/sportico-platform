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

        /// <summary>
        /// Adds the given notifications and saves ONLY them. Intended for side-effect
        /// notifications raised after the authoritative business mutation has already been
        /// committed. Never throws: on failure it detaches the un-saved notifications (so the
        /// shared DbContext stays clean) and returns the caught exception for the caller to log.
        /// Returns null on success.
        /// </summary>
        Task<Exception?> TryAddAndSaveAsync(IReadOnlyCollection<Notification> notifications);
    }
}
