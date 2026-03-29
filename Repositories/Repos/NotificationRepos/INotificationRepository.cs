using Repositories.Entities;

namespace Repositories.Repos.NotificationRepos
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification);
        Task<List<Notification>> GetByUserIdAsync(int targetUserId);
    }
}
