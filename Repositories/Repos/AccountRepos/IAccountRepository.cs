using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.AccountRepos
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetFashionExperts();
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetRefreshTokenByAccountIdAsync(int accountId);
        Task UpdateRefreshTokenAsync(RefreshToken token);
    }
}
