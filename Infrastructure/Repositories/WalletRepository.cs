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