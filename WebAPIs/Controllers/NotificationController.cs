using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.NotificationImp;
using System.Security.Claims;

namespace WebAPIs.Controllers
{
    namespace WebAPIs.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        [Authorize]
        public class NotificationController : ControllerBase
        {
            private readonly INotificationService _notificationService;

            public NotificationController(INotificationService notificationService)
            {
                _notificationService = notificationService;
            }

            [HttpGet("my-notifications")]
            public async Task<IActionResult> GetMyNotifications()
            {
                try
                {
                    var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                       ?? User.FindFirst("AccountId")?.Value;

                    if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                    {
                        return Unauthorized(new { message = "Invalid token" });
                    }

                    var result = await _notificationService.GetMyNotificationsAsync(userId);

                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = ex.Message });
                }
            }
        }
    }
}
