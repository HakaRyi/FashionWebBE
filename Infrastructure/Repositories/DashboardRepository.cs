using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly FashionDbContext _db;
        public DashboardRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<List<Account>> Get3NewestUser()
        {
            return await _db.Accounts
                .Include(a => a.Avatars)
                .Where(p => p.Status == "Active" && _db.UserRoles.Any(ur => ur.UserId == p.Id && ur.RoleId == 2))
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();
        }

        public IQueryable<Account> GetAccountsByRole(int roleId)
        {
            return _db.Accounts
                .Where(p => p.Status == "Active" && _db.UserRoles.Any(ur => ur.UserId == p.Id && ur.RoleId == roleId))
  ;
        }

        public IQueryable<Notification> GetAdminNotificationsQuery()
        {
            return _db.Notifications
                .Include(n => n.Sender).ThenInclude(n => n.Avatars)
                .Where(n => n.TargetUserId == 1)
                .OrderByDescending(n => n.CreatedAt);
        }
        public IQueryable<Event> GetEvents()
        {
            return _db.Events
                .Include(e => e.Creator)
                .Include(e => e.Images)
                .Include(e => e.Posts)
                .Include(e => e.PrizeEvents)
                .Include(e => e.EventExperts)
                    .ThenInclude(ee => ee.Expert)
                .OrderByDescending(e => e.StartTime);
        }

        public IQueryable<Post> GetPosts()
        {
            return _db.Posts
                 .Where(p => p.Status == "Published");
        }

        public async Task<List<Transaction>> GetRevenueSystem()
        {
            return await _db.Transactions
                .Include(a => a.Wallet).ThenInclude(w => w.Account)
                .Where(t => (t.Type == "PayForVTON" || t.Type == "PayForAISuggest") && t.Status == "Success")
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public IQueryable<Transaction> GetRevenueTransactions()
        {
            return _db.Transactions
                 .Where(t => (t.Type == "System_Fee_Revenue" ||
                 (t.Type == "Debit" && (t.ReferenceType == "AIRecommendation" || t.ReferenceType == "TryOn"))) 
                 && t.Status == "Success");
        }
    }
}
