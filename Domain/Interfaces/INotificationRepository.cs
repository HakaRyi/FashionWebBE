using Domain.Entities;

namespace Domain.Interfaces

{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification);
        Task<List<Notification>> GetByUserIdAsync(int targetUserId);
    }
}
