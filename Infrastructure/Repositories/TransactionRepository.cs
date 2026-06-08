using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Linq.Expressions;

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

        public async Task<List<Transaction>> GetTransactionsAsync(string? type = null, string? refType = null, int? refId = null, params Expression<Func<Transaction, object>>[] includes)
        {
            IQueryable<Transaction> query = _db.Transactions;
            foreach (var include in includes) query = query.Include(include);

            if (!string.IsNullOrEmpty(type)) query = query.Where(t => t.Type == type);
            if (!string.IsNullOrEmpty(refType)) query = query.Where(t => t.ReferenceType == refType);
            if (refId.HasValue) query = query.Where(t => t.ReferenceId == refId);

            return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
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
                .Include(t => t.Wallet)
                .ThenInclude(w => w.Account)
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetMonthlyDebitTotalAsync(int walletId, int month, int year)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId
                         && t.Type == TransactionType.Debit
                         && t.Status == TransactionStatus.Success
                         && t.CreatedAt.Month == month
                         && t.CreatedAt.Year == year)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        public async Task<List<Transaction>> GetByReferenceAsync(string refType, int refId)
        {
            return await _db.Transactions
                .Include(t => t.Wallet)
                .ThenInclude(w => w.Account)
                .Where(t => t.ReferenceType == refType && t.ReferenceId == refId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetTransactionsForDashboardAsync(DateTime startDate, DateTime endDate)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .Where(t => t.Status == "Success" && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetAllTransactionsByOrderIdAsync(int orderId)
        {
            return await _db.Set<Transaction>()
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .Where(t => t.ReferenceId == orderId
                       && (t.ReferenceType == "OrderPayment" || t.ReferenceType == "OrderRefund")
                       && t.Status == "Success")
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetAllTransactionsAsync(DateTime fromDate, DateTime toDate)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .Include(t => t.EscrowSession)
                .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByWalletIdAsync(int walletId)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .Include(t => t.EscrowSession)
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
        public async Task<List<Transaction>> GetRevenueTransactionsWithDetailsAsync(DateTime start, DateTime end)
        {
            return await _db.Transactions
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .Include(t => t.EscrowSession)
                    .ThenInclude(e => e.Sender)
                .Where(t => t.Status == "Success" && t.CreatedAt >= start && t.CreatedAt <= end)
                .Where(t => (t.Type == "Credit" && t.WalletId == 1) ||
                            (t.Type == "Debit" && (t.ReferenceType == "AIRecommendation" || t.ReferenceType == "TryOn")))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}