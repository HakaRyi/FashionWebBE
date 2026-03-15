using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.AccountSubscriptionRepos
{
    public interface IAccountSubscriptionRepository
    {
        Task AddAsync(AccountSubscription subscription);
        Task<AccountSubscription?> GetLatestActiveSubscriptionAsync(int accountId);
    }
}
