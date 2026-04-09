using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Infrastructure.Repositories
{
    public class WardrobeRepository : IWardrobeRepository
    {
        private readonly FashionDbContext _db;

        public WardrobeRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<int> CreateWardrobe(Wardrobe wardrobe)
        {
            _db.Wardrobes.Add(wardrobe);
            return await _db.SaveChangesAsync();
        }

        public async Task<List<Wardrobe>> GetAll()
        {
            return await _db.Wardrobes
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Wardrobe?> GetByIdAsync(int wardrobeId)
        {
            return await _db.Wardrobes
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WardrobeId == wardrobeId);
        }

        public async Task<Wardrobe?> GetByAccountIdAsync(int accountId)
        {
            return await _db.Wardrobes
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.AccountId == accountId);
        }
        public async Task<List<Account>> SearchAccountWithWardrobeAsync(string username, int limit = 5)
        {
            return await _db.Accounts
                .Include(a => a.Wardrobe)
                .Include(a => a.Avatars)
                .Where(a => a.UserName.Contains(username))
                .OrderBy(a => a.UserName)
                .Take(limit)
                .ToListAsync();
        }
    }
}