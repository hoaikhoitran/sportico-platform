using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task<Result<PagedResult<NotificationResponse>>> GetMyAsync(
            Guid userId,
            NotificationFilterRequest filter);

        Task<Result<int>> GetUnreadCountAsync(Guid userId);

        Task<Result<object>> MarkReadAsync(Guid userId, Guid notificationId);

        Task<Result<object>> MarkAllReadAsync(Guid userId);
    }
}
