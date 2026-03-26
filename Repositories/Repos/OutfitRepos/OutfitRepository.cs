using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.OutfitRepos
{
    public class OutfitRepository : IOutfitRepository
    {
        private readonly FashionDbContext _context;
        public OutfitRepository(FashionDbContext context)
        { 
            _context = context;
        }
        public async Task AddAsync(Outfit outfit)
        {
            await _context.Outfits.AddAsync(outfit);
            await _context.SaveChangesAsync();
        }

        public async Task<Outfit> CreateOutfitAsync(Outfit outfit)
        {
            outfit.CreatedAt = DateTime.UtcNow;

            await _context.Outfits.AddAsync(outfit);
            await _context.SaveChangesAsync();

            return outfit;
        }

        public async Task<List<Outfit>> GetUserOutfitsAsync(int accountId)
        {
            return await _context.Outfits
                .Include(o => o.OutfitItems)
                    .ThenInclude(oi => oi.Item)
                        .ThenInclude(i => i.Images)
                .Where(o => o.AccountId == accountId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}
