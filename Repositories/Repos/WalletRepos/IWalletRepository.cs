using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.WalletRepos
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByAccountIdAsync(int accountId);
        Task UpdateBalanceAsync(int walletId, decimal balance, decimal lockedBalance);
        Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(int walletId);
        void Update(Wallet wallet);
    }
}
