using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByIdAsync(int walletId);
        Task<Wallet?> GetByAccountIdAsync(int accountId);
        Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(int walletId);
        Task<IEnumerable<Transaction>> GetWalletTransactionsAsync(int walletId, List<string> Types);
        Task AddAsync(Wallet wallet);
        void Update(Wallet wallet); 
        IQueryable<Wallet> Query();
    }
}