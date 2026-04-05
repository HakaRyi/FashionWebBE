using Microsoft.AspNetCore.SignalR;
using Domain.Entities;
using Application.Request.NotificationReq;
using Application.Response.NotificationResp;
using Application.Utils.SignalR;
using Domain.Interfaces;

namespace Application.Services.NotificationImp
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            INotificationRepository repository,
            IHubContext<NotificationHub> hubContext)
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
                await _hubContext.Clients
                    .User(request.TargetUserId.Value.ToString())
                    .SendAsync("ReceiveNotification", new
                    {
                        notification.NotificationId,
                        notification.Title,
                        notification.Content,
                        notification.Type,
                        notification.Status,
                        notification.CreatedAt
                    });
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
                CreatedAt = n.CreatedAt ?? DateTime.UtcNow
            }).ToList();
        }

        public async Task SendWalletUpdatedAsync(int userId, object payload)
        {
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("WalletBalanceUpdated", payload);
        }
    }
}