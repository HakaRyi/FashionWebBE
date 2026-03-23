using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.TransactionRepos
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly FashionDbContext _db;
        public TransactionRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<Transaction?> GetById(int id)
        {
            return await _db.Transactions
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .FirstOrDefaultAsync(tr => tr.TransactionId == id);
        }

        public async Task<List<Transaction>> GetTransactions()
        {
            return await _db.Transactions
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _db.Transactions.AddAsync(transaction);
        }

        public async Task<List<Transaction>> GetHistoryByWalletIdAsync(int walletId)
            => await _db.Transactions
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Transaction>> GetByWalletIdAsync(int walletId) =>
        await _db.Transactions.Where(t => t.WalletId == walletId).ToListAsync();

        public IQueryable<Transaction> Query()
        {
            return _db.Transactions
                .AsNoTracking()
                .Include(x => x.Wallet);
        }

        public async Task<Transaction?> GetByIdWithWalletAsync(int transactionId)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Include(x => x.Wallet)
                .FirstOrDefaultAsync(x => x.TransactionId == transactionId);
        }
    }
}

