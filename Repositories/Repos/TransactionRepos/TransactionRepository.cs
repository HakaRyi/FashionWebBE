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

        public async Task<Transaction> GetById(int id)
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
    }
}
