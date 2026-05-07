using Application.Request.NotificationReq;
using Application.Response.NotificationResp;
using Application.Utils.SignalR;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

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
                CreatedAt = DateTime.UtcNow,
                RelatedId = int.TryParse(request.RelatedId, out var relatedId)
                    ? relatedId
                    : null
            };

            await _repository.CreateAsync(notification);

            if (request.TargetUserId.HasValue)
            {
                await _hubContext.Clients
                    .User(request.TargetUserId.Value.ToString())
                    .SendAsync("ReceiveNotification", new NotificationResponse
                    {
                        Id = notification.NotificationId,
                        Title = notification.Title,
                        Content = notification.Content,
                        Type = notification.Type,
                        Status = notification.Status,
                        CreatedAt = notification.CreatedAt ?? DateTime.UtcNow,
                        RelatedId = notification.RelatedId
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
                CreatedAt = n.CreatedAt ?? DateTime.UtcNow,
                RelatedId = n.RelatedId
            }).ToList();
        }

        public async Task SendWalletUpdatedAsync(int userId, object payload)
        {
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("WalletBalanceUpdated", payload);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _repository.GetByIdAsync(notificationId);

            if (notification == null)
            {
                return false;
            }

            if (notification.TargetUserId != userId)
            {
                return false;
            }

            if (string.Equals(notification.Status, "Read", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            notification.Status = "Read";

            await _repository.UpdateAsync(notification);

            return true;
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await _repository.MarkAllAsReadAsync(userId);
        }
    }
}