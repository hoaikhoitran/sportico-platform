using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class NotificationMappingExtensions
    {
        public static NotificationResponse ToResponse(
            this Notification notification)
        {
            return new NotificationResponse
            {
                Id = notification.Id,
                Title = notification.Title,
                Content = notification.Content,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }
    }
}