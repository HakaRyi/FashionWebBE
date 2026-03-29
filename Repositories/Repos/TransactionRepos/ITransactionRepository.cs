using Repositories.Entities;

namespace Repositories.Repos.TransactionRepos
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(int transactionId);
        Task<Transaction?> GetByIdWithWalletAsync(int transactionId);
        Task<List<Transaction>> GetTransactionsAsync();
        Task<List<Transaction>> GetHistoryByWalletIdAsync(int walletId);
        Task<IEnumerable<Transaction>> GetByWalletIdAsync(int walletId);

        Task AddAsync(Transaction transaction);

        IQueryable<Transaction> Query();
    }
}