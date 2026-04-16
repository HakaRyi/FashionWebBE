using Application.Request.NotificationReq;
using Application.Response.NotificationResp;

namespace Application.Services.NotificationImp
{
    public interface INotificationService
    {
        Task SendNotificationAsync(SendNotificationRequest request);
        Task<List<NotificationResponse>> GetMyNotificationsAsync(int userId);
        Task SendWalletUpdatedAsync(int userId, object payload);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
    }
}