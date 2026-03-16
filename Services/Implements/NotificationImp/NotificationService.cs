using Microsoft.AspNetCore.SignalR;
using Repositories.Entities;
using Repositories.Repos.NotificationRepos;
using Services.Request.NotificationReq;
using Services.Response.NotificationResp;
using Services.Utils.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.NotificationImp
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(INotificationRepository repository, IHubContext<NotificationHub> hubContext)
        {
            _repository = repository;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(SendNotificationRequest request)
        {
            var notification = new Notification
            {
                SenderId = request.SenderId,
                TargetUserId = request.TargetUserId,
                Title = request.Title,
                Content = request.Content,
                Type = request.Type,
                Status = "Unread",
                CreatedAt = DateTime.UtcNow
            };

            await _repository.CreateAsync(notification);

            if (request.TargetUserId.HasValue)
            {
                await _hubContext.Clients.User(request.TargetUserId.Value.ToString()).SendAsync("ReceiveNotification", notification);
            }
        }

        public async Task<List<NotificationResponse>> GetMyNotificationsAsync(int userId)
        {
            var notifications = await _repository.GetByUserIdAsync(userId);

            return notifications.Select(n => new NotificationResponse
            {
                Id = n.NotificationId,
                Title = n.Title,
                Content = n.Content,
                Type = n.Type,
                Status = n.Status,
                CreatedAt = (DateTime)n.CreatedAt
            }).ToList();
        }
    }
}
