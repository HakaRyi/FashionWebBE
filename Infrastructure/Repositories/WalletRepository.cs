using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly FashionDbContext _context;

        public WalletRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<Wallet?> GetByIdAsync(int walletId)
        {
            return await _context.Wallets
                .FirstOrDefaultAsync(x => x.WalletId == walletId);
        }

        public async Task<Wallet?> GetByAccountIdAsync(int accountId)
        {
            return await _context.Wallets
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.AccountId == accountId);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(int walletId)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetWalletTransactionsAsync(int walletId, List<string> referenceTypes)
        {
            return await _context.Transactions
                .Include(t => t.Payment)
                .Where(t => t.WalletId == walletId && referenceTypes.Contains(t.ReferenceType))
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddAsync(Wallet wallet)
        {
            await _context.Wallets.AddAsync(wallet);
        }

        public void Update(Wallet wallet)
        {
            _context.Wallets.Update(wallet);
        }

        public IQueryable<Wallet> Query()
        {
            return _context.Wallets.AsQueryable();
        }
    }
}