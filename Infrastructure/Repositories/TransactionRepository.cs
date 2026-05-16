using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
                .AsNoTracking()
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

        public async Task<List<Transaction>> GetTransactionsAsync(
            string? type = null,
            string? refType = null,
            int? refId = null,
            string? search = null,
            string? searchBy = null,
            params Expression<Func<Transaction, object>>[] includes)
        {
            IQueryable<Transaction> query = _db.Transactions.AsNoTracking();

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(t => t.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(refType))
            {
                query = query.Where(t => t.ReferenceType == refType);
            }

            if (refId.HasValue)
            {
                query = query.Where(t => t.ReferenceId == refId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                string mode = string.IsNullOrWhiteSpace(searchBy)
                    ? "all"
                    : searchBy.Trim().ToLower();

                query = mode switch
                {
                    "transactioncode" => query.Where(t =>
                        t.TransactionCode != null &&
                        t.TransactionCode.ToLower().Contains(keyword)),

                    "username" => query.Where(t =>
                        t.Wallet != null &&
                        t.Wallet.Account != null &&
                        t.Wallet.Account.UserName != null &&
                        t.Wallet.Account.UserName.ToLower().Contains(keyword)),

                    "description" => query.Where(t =>
                        t.Description != null &&
                        t.Description.ToLower().Contains(keyword)),

                    "referenceid" => int.TryParse(keyword, out int parsedRefId)
                        ? query.Where(t => t.ReferenceId == parsedRefId)
                        : query.Where(t => false),

                    "ordercode" => query.Where(t =>
                        _db.Orders.Any(o =>
                            o.OrderCode != null &&
                            o.OrderCode.ToLower().Contains(keyword) &&
                            o.OrderId == t.ReferenceId &&
                            (
                                t.ReferenceType == "OrderPayment" ||
                                t.ReferenceType == "OrderRefund"
                            ))),

                    _ => query.Where(t =>
                        (
                            t.TransactionCode != null &&
                            t.TransactionCode.ToLower().Contains(keyword)
                        ) ||
                        (
                            t.Description != null &&
                            t.Description.ToLower().Contains(keyword)
                        ) ||
                        (
                            t.Wallet != null &&
                            t.Wallet.Account != null &&
                            t.Wallet.Account.UserName != null &&
                            t.Wallet.Account.UserName.ToLower().Contains(keyword)
                        ) ||
                        _db.Orders.Any(o =>
                            o.OrderCode != null &&
                            o.OrderCode.ToLower().Contains(keyword) &&
                            o.OrderId == t.ReferenceId &&
                            (
                                t.ReferenceType == "OrderPayment" ||
                                t.ReferenceType == "OrderRefund"
                            )))
                };
            }

            return await query
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
                .Where(t =>
                    t.WalletId == walletId &&
                    t.Type == TransactionType.Debit &&
                    t.Status == TransactionStatus.Success &&
                    t.CreatedAt.Month == month &&
                    t.CreatedAt.Year == year)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        public async Task<List<Transaction>> GetByReferenceAsync(string refType, int refId)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Account)
                .Where(t => t.ReferenceType == refType && t.ReferenceId == refId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<int, string?>> GetOrderCodeMapByOrderIdsAsync(List<int> orderIds)
        {
            if (orderIds == null || orderIds.Count == 0)
            {
                return new Dictionary<int, string?>();
            }

            return await _db.Orders
                .AsNoTracking()
                .Where(o => orderIds.Contains(o.OrderId))
                .ToDictionaryAsync(
                    o => o.OrderId,
                    o => o.OrderCode
                );
        }

        public async Task<Dictionary<int, string?>> GetEventNameMapByEventIdsAsync(List<int> eventIds)
        {
            if (eventIds == null || eventIds.Count == 0)
            {
                return new Dictionary<int, string?>();
            }

            return await _db.Events
                .AsNoTracking()
                .Where(e => eventIds.Contains(e.EventId))
                .ToDictionaryAsync(
                    e => e.EventId,
                    e => e.Title
                );
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