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
                .Include(t => t.Account)
                .FirstOrDefaultAsync(tr => tr.TransactionId == id);
        }

        public async Task<List<Transaction>> GetTransactions()
        {
            return await _db.Transactions
                .Include(t => t.Account)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

        }

        public async Task AddAsync(Transaction transaction) => await _db.Transactions.AddAsync(transaction);

        public async Task<IEnumerable<Transaction>> GetHistoryByAccountIdAsync(int accountId)
            => await _db.Transactions.Where(t => t.AccountId == accountId).OrderByDescending(t => t.CreatedAt).ToListAsync();

        public async Task<int> GetCurrentBalanceAsync(int accountId)
        {
            var lastTransaction = await _db.Transactions
                .Where(t => t.AccountId == accountId && t.Status == "Success")
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
            return lastTransaction?.BalanceAfter ?? 0;
        }
    }
}
