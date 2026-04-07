using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByIdAsync(int walletId);
        Task<Wallet?> GetByAccountIdAsync(int accountId);
        Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(int walletId);

        Task AddAsync(Wallet wallet);
        void Update(Wallet wallet);

        IQueryable<Wallet> Query();
    }
}