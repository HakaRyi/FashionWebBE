using Repositories.Entities;

namespace Repositories.Repos.WalletRepos
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByAccountIdAsync(int accountId);
        Task UpdateBalanceAsync(int walletId, decimal balance, decimal lockedBalance);
        Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(int walletId);
        void Update(Wallet wallet);
        Task CreateWalletAsync(Wallet wallet);
        Task UpdateWalletAsync(Wallet wallet);
        IQueryable<Wallet> Query();
        Task<Wallet?> GetByIdAsync(int walletId);
    }
}
