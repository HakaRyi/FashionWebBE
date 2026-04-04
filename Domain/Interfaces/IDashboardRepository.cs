using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IDashboardRepository
    {
        IQueryable<Account> GetAccountsByRole(int roleId);
        IQueryable<Post> GetPosts();
        IQueryable<Transaction> GetRevenueTransactions();
        Task<List<Transaction>> GetRevenueSystem();
        Task<List<Account>> Get3NewestUser();
        IQueryable<Notification> GetAdminNotificationsQuery();
        IQueryable<Event> GetEvents();
    }
}
