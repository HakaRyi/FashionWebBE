using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly FashionDbContext _db;

        public TransactionRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<Transaction?> GetByIdAsync(int transactionId)
        {
            return await _db.Transactions
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<Transaction?> GetByIdWithWalletAsync(int transactionId)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<List<Transaction>> GetTransactionsAsync()
        {
            return await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetHistoryByWalletIdAsync(int walletId)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetByWalletIdAsync(int walletId)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _db.Transactions.AddAsync(transaction);
        }

        public IQueryable<Transaction> Query()
        {
            return _db.Transactions
                .AsNoTracking()
                .Include(t => t.Wallet);
        }
    }
}