using Repositories.Data;
using Repositories.Entities;

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
    }
}
