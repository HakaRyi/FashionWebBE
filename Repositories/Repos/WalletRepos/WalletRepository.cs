using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.WalletRepos
{
    public class WalletRepository : IWalletRepository
    {
        private readonly FashionDbContext _context;
        public WalletRepository(FashionDbContext context) => _context = context;

        public async Task<Wallet?> GetByAccountIdAsync(int accountId)
        {
            return await _context.Wallets
                .FirstOrDefaultAsync(w => w.AccountId == accountId);
        }

        public async Task UpdateBalanceAsync(int walletId, decimal balance, decimal lockedBalance)
        {
            var wallet = await _context.Wallets.FindAsync(walletId);
            if (wallet != null)
            {
                wallet.Balance = balance;
                wallet.LockedBalance = lockedBalance;
                wallet.UpdatedAt = DateTime.Now;
            }
        }

        public async Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(int walletId)
        {
            return await _context.Transactions
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public void Update(Wallet wallet)
        {
            _context.Wallets.Update(wallet);
        }

        public async Task CreateWalletAsync(Wallet wallet)
        {
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateWalletAsync(Wallet wallet)
        {
            _context.Wallets.Update(wallet);
            await _context.SaveChangesAsync();
        }
    }
}
