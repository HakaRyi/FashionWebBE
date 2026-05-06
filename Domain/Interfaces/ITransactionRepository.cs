using Domain.Entities;
using System.Linq.Expressions;

namespace Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(int transactionId);
        Task<Transaction?> GetByIdWithWalletAsync(int transactionId);
        Task<List<Transaction>> GetTransactionsAsync(string? type = null, string? refType = null, int? refId = null, params Expression<Func<Transaction, object>>[] includes);
        Task<List<Transaction>> GetHistoryByWalletIdAsync(int walletId);
        Task<IEnumerable<Transaction>> GetByWalletIdAsync(int walletId);
        Task<decimal> GetMonthlyDebitTotalAsync(int walletId, int month, int year);
        Task<List<Transaction>> GetByReferenceAsync(string refType, int refId);
        Task AddAsync(Transaction transaction);
        IQueryable<Transaction> Query();
    }
}