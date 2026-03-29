using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.AccountSubscriptionRepos
{
    public class AccountSubscriptionRepository : IAccountSubscriptionRepository
    {
        private readonly FashionDbContext _context;

        public AccountSubscriptionRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AccountSubscription subscription)
        {
            await _context.AccountSubscriptions.AddAsync(subscription);
        }

        public async Task<AccountSubscription?> GetLatestActiveSubscriptionAsync(int accountId)
        {
            return await _context.AccountSubscriptions
                .Where(s => s.AccountId == accountId && s.IsActive && s.EndDate > DateTime.UtcNow)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }
    }
}