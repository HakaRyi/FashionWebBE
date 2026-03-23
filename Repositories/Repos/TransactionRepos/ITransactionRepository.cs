using Repositories.Entities;

namespace Repositories.Repos.TransactionRepos
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetById(int id);
        Task<List<Transaction>> GetTransactions();
        Task AddAsync(Transaction transaction);
        Task<List<Transaction>> GetHistoryByWalletIdAsync(int walletId);
        Task<IEnumerable<Transaction>> GetByWalletIdAsync(int walletId);

        IQueryable<Transaction> Query();
        Task<Transaction?> GetByIdWithWalletAsync(int transactionId);
    }
}
