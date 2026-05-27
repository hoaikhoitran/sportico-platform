using FluentValidation;
using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<NotificationFilterRequest> _filterValidator;

        public NotificationService(
            INotificationRepository notificationRepository,
            IValidator<NotificationFilterRequest> filterValidator)
        {
            _notificationRepository = notificationRepository;
            _filterValidator = filterValidator;
        }

        public async Task<Result<PagedResult<NotificationResponse>>> GetMyAsync(
            Guid userId,
            NotificationFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var (items, totalCount) = await _notificationRepository.GetPagedByUserIdAsync(userId, filter);

            var response = new PagedResult<NotificationResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<NotificationResponse>>.Success(response);
        }

        public async Task<Result<int>> GetUnreadCountAsync(Guid userId)
        {
            var count = await _notificationRepository.GetUnreadCountAsync(userId);

            return Result<int>.Success(count);
        }

        public async Task<Result<object>> MarkReadAsync(Guid userId, Guid notificationId)
        {
            var notification = await _notificationRepository.GetByIdForUpdateAsync(userId, notificationId);
            if (notification == null)
            {
                throw new NotFoundException(
                    ErrorCodes.NotificationNotFound,
                    "Notification not found");
            }

            notification.IsRead = true;

            await _notificationRepository.SaveChangesAsync();

            return Result<object>.Success(new { status = "ok" });
        }

        public async Task<Result<object>> MarkAllReadAsync(Guid userId)
        {
            var unread = await _notificationRepository.GetUnreadForUpdateAsync(userId);

            foreach (var notification in unread)
            {
                notification.IsRead = true;
            }

            await _notificationRepository.SaveChangesAsync();

            return Result<object>.Success(new { status = "ok" });
        }
    }
}
