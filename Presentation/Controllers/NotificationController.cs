using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Services.NotificationImp;

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

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int userId = _currentUserService.GetRequiredUserId();

            var result = await _notificationService.MarkAsReadAsync(id, userId);

            if (!result) return NotFound(new { message = "Không tìm thấy thông báo hoặc bạn không có quyền" });

            return Ok();
        }
    }
}