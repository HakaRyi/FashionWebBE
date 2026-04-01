using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.WardrobeRepos
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

        public async Task<Wardrobe?> GetById(int accountId)
        {
            return await _db.Wardrobes
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.AccountId == accountId);
        }

        public async Task<Wardrobe?> GetByAccountIdAsync(int accountId)
        {
            return await _db.Wardrobes
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.AccountId == accountId);
        }
    }
}