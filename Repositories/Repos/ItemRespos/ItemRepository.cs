using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ItemRespos
{
    public class ItemRepository : IItemRepository
    {
        private readonly FashionDbContext _context;

        public ItemRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items
                .Include(i => i.Images)
                .Include(i => i.Categories)
                .FirstOrDefaultAsync(i => i.ItemId == id);
        }

        public async Task<IEnumerable<Item>> GetAllAsync()
        {
            return await _context.Items
                .AsNoTracking()
                .Include(i => i.Images)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetByWardrobeIdAsync(int wardrobeId)
        {
            return await _context.Items
                .AsNoTracking()
                .OrderByDescending(i => i.CreatedAt)
                .Include(i => i.Images)
                .Where(i => i.WardrobeId == wardrobeId)
                .ToListAsync();
        }

        public async Task<List<Item>> GetByVectorSimilarityAsync(Vector embedding, int limit = 20)
        {

            return await _context.Items
                .AsNoTracking()
                .OrderBy(i => i.ItemEmbedding.CosineDistance(embedding))
                .Take(limit)
                .Select(i => new Item
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    MainColor = i.MainColor,
                    Style = i.Style,
                    Images = i.Images.Take(1).ToList()
                })
                .ToListAsync();
        }

        public async Task AddAsync(Item item)
        {
            item.CreatedAt = DateTime.UtcNow;
            await _context.Items.AddAsync(item);
        }

        public void Update(Item item)
        {
            item.UpdateAt = DateTime.UtcNow;
            _context.Items.Update(item);
        }

        public void Delete(Item item)
        {
            _context.Items.Remove(item);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
