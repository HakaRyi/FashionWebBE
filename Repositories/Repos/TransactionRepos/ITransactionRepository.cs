using Repositories.Entities;

namespace Repositories.Repos.TransactionRepos
{
    public interface ITransactionRepository
    {
        Task<Transaction> GetById(int id);
        Task<List<Transaction>> GetTransactions();
    }
}
