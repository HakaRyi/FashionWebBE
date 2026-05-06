using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly FashionDbContext _context;

        public NotificationRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<List<Notification>> GetByUserIdAsync(int targetUserId)
        {
            return await _context.Notifications
                 .Where(n => n.TargetUserId == targetUserId)
                 .OrderByDescending(n => n.CreatedAt)
                 .ToListAsync();
        }

        public async Task<Notification?> GetById(int id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task Update(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.TargetUserId == userId && n.Status == "Unread")
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.Status = "Read";
            }

            await _context.SaveChangesAsync();
        }
    }
}
