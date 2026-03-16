using Services.Request.NotificationReq;
using Services.Response.NotificationResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.NotificationImp
{
    public interface INotificationService
    {
        Task SendNotificationAsync(SendNotificationRequest request);
        Task<List<NotificationResponse>> GetMyNotificationsAsync(int userId);
    }
}
