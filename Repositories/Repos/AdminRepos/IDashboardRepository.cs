using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.AdminRepos
{
    public interface IDashboardRepository
    {
        IQueryable<Account> GetAccountsByRole(int roleId);
        IQueryable<Post> GetPosts();
        IQueryable<Transaction> GetRevenueTransactions();
        Task<List<Transaction>> GetRevenueSystem();
        Task<List<Account>> Get3NewestUser();
        IQueryable<Notification> GetAdminNotificationsQuery();
    }
}
