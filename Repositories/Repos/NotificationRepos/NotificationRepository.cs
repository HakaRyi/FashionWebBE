using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.NotificationRepos
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
    }
}
