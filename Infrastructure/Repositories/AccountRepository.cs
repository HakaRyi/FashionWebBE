using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;

namespace Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly FashionDbContext _db;

        public AccountRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<List<Account>> GetFashionExperts()
        {
            return await _db.Accounts
                .Include(a => a.ExpertProfile)
                    .ThenInclude(ep => ep.ExpertRequests)
                .Include(a => a.Avatars)
                .Where(a => a.ExpertProfile != null)
                .ToListAsync();
        }

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

        public async Task<int> UpdateAccount(Account account)
        {
            _db.Accounts.Update(account);
            return await _db.SaveChangesAsync();
        }

        public async Task<Account?> GetAccountById(int userId)
        {
            return await _db.Accounts
                .FirstOrDefaultAsync(a => a.Id == userId);
        }

        public async Task<List<Account>> GetAll()
        {
            return await _db.Accounts
                .Include(a => a.Avatars)
                .Where(p => p.Status != "" && _db.UserRoles.Any(ur => ur.UserId == p.Id && ur.RoleId != 1))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<RefreshToken?> GetRefreshTokenByTokenAsync(string token)
        {
            return await _db.RefreshTokens
                .Include(rt => rt.Account)
                .FirstOrDefaultAsync(x => x.Token == token);
        }

        public async Task<Account?> GetAccountWithProfileAndAvatarsAsync(int accountId)
        {
            return await _db.Accounts
                .Include(a => a.ExpertProfile)
                .Include(a => a.Avatars)
                .FirstOrDefaultAsync(a => a.Id == accountId);
        }
    }
}