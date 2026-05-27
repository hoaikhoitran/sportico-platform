using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(Result<PagedResult<NotificationResponse>>), 200)]
        public async Task<IActionResult> GetMy([FromQuery] NotificationFilterRequest filter)
        {
            var userId = User.GetUserId();
            var result = await _notificationService.GetMyAsync(userId, filter);
            return Ok(result);
        }

        [HttpGet("me/unread-count")]
        [ProducesResponseType(typeof(Result<int>), 200)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.GetUserId();
            var result = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(result);
        }

        [HttpPut("{id:guid}/read")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _notificationService.MarkReadAsync(userId, id);
            return Ok(result);
        }

        [HttpPut("me/read-all")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.GetUserId();
            var result = await _notificationService.MarkAllReadAsync(userId);
            return Ok(result);
        }
    }
}
