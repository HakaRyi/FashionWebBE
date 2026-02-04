using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.AccountRepos
{
    public class AccountRepository : IAccountRepository
    {
        private readonly FashionDbContext _db;

        public AccountRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<Account?> GetAccountByEmail(string email)
        {
            return await _db.Accounts.Include(x => x.Role)
                                     .FirstOrDefaultAsync(x => x.Email == email);
        }

        public Task<Account?> GetAccountById(int accountId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Account>> GetAllAccounts()
        {
            throw new NotImplementedException();
        }

        public Task<Account?> SignIn(string email, string password)
        {
            throw new NotImplementedException();
        }

        public async Task<Account> SignUp(Account account)
        {
            await _db.Accounts.AddAsync(account);
            await _db.SaveChangesAsync();
            return account;
        }

        public Task<bool> UpdateAccount(Account account)
        {
            throw new NotImplementedException();
        }

        // ====================================Refresh Token methods===================================
        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _db.RefreshTokens.AddAsync(refreshToken);
            await _db.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetRefreshTokenByAccountIdAsync(int accountId)
        {
            return await _db.RefreshTokens.FirstOrDefaultAsync(x => x.AccountId == accountId);
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken token)
        {
            _db.RefreshTokens.Update(token);
            await _db.SaveChangesAsync();
        }
    }
}
