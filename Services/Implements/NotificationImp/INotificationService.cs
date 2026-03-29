using Services.Request.NotificationReq;
using Services.Response.NotificationResp;

namespace Services.Implements.NotificationImp
{
    public interface INotificationService
    {
        Task SendNotificationAsync(SendNotificationRequest request);
        Task<List<NotificationResponse>> GetMyNotificationsAsync(int userId);
        Task SendWalletUpdatedAsync(int userId, object payload);
    }
}