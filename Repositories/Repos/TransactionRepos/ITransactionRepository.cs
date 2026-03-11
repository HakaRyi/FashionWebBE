using Repositories.Entities;

namespace Repositories.Repos.TransactionRepos
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetById(int id);
        Task<List<Transaction>> GetTransactions();
        Task AddAsync(Transaction transaction);
        Task<IEnumerable<Transaction>> GetHistoryByAccountIdAsync(int accountId);
        Task<int> GetCurrentBalanceAsync(int accountId);
    }
}
