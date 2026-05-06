using Application.Interfaces;
using Application.Services.NotificationImp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public NotificationsController(
            INotificationService notificationService,
            ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyNotifications()
        {
            int userId = _currentUserService.GetRequiredUserId();

            var result = await _notificationService.GetMyNotificationsAsync(userId);

            return Ok(result);
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int userId = _currentUserService.GetRequiredUserId();

            var isUpdated = await _notificationService.MarkAsReadAsync(id, userId);

            if (!isUpdated)
            {
                return NotFound(new
                {
                    message = "Notification not found, or you do not have permission."
                });
            }

            return Ok(new
            {
                message = "Notification has been marked as read."
            });
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int userId = _currentUserService.GetRequiredUserId();

            await _notificationService.MarkAllAsReadAsync(userId);

            return Ok(new
            {
                message = "All notifications have been marked as read."
            });
        }
    }
}