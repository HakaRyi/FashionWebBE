using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.AccountRepos
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
                    .ThenInclude(ep => ep.ExpertFile)
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
    }
}