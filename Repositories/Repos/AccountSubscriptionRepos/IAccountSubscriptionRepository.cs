using Repositories.Entities;

namespace Repositories.Repos.AccountSubscriptionRepos
{
    public interface IAccountSubscriptionRepository
    {
        Task AddAsync(AccountSubscription subscription);
        Task<AccountSubscription?> GetLatestActiveSubscriptionAsync(int accountId);
    }
}
