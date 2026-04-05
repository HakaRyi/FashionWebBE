using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

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
    }
}