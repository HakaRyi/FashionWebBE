using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Dto;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Repositories.Repos.ItemRespos
{
    public class ItemRepository: IItemRepository
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
                .FirstOrDefaultAsync(i => i.ItemId == id);
        }

        public async Task<List<Item>> GetItemsByIds(List<int> itemIds)
        {
            return await _context.Items
                .Where(i => itemIds.Contains(i.ItemId))
                .ToListAsync();
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
                    Images = i.Images.Take(1).ToList()
                })
                .ToListAsync();
        }

        public async Task<List<Item>> GetHybridRecommendationsAsync(
            Vector queryVector,
            SearchIntent intent,
            int currentAccountId,
            SmartRecommendationDto scopeRequest)
        {
            var query = _context.Items.AsQueryable();

            // --- BƯỚC 1: LỌC THEO PHẠM VI (SCOPE) ---
            query = query.Where(item =>
                (scopeRequest.UseMyWardrobe && item.Wardrobe.AccountId == currentAccountId) ||
                (scopeRequest.UseSavedItems && item.SavedByUsers.Any(s => s.AccountId == currentAccountId)) ||
                (scopeRequest.UseCommunityItems && item.IsPublic == true && item.Wardrobe.AccountId != currentAccountId)
            );

            if (!string.IsNullOrEmpty(scopeRequest.ReferenceCategory))
            {
                query = query.Where(i => i.Category != scopeRequest.ReferenceCategory);

                if (scopeRequest.ReferenceCategory.Equals("full_body", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.Category != "upper_body" && i.Category != "lower_body");
                }

                if (scopeRequest.ReferenceCategory.Equals("upper_body", StringComparison.OrdinalIgnoreCase) ||
                    scopeRequest.ReferenceCategory.Equals("lower_body", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.Category != "full_body");
                }
            }

            // --- BƯỚC 2: LỌC CỨNG THEO METADATA TỪ GEMINI (EXACT MATCH) ---
            if (!string.IsNullOrEmpty(intent.Gender) && intent.Gender != "Unknown")
            {
                query = query.Where(i => i.Gender == intent.Gender || i.Gender == "Unisex");
            }

            // [QUAN TRỌNG]: Fix lỗi AI ảo giác trả về Category trùng nhau
            if (!string.IsNullOrEmpty(intent.Category) && intent.Category != "Unknown")
            {
                bool isAiHallucinating = !string.IsNullOrEmpty(scopeRequest.ReferenceCategory) &&
                                         intent.Category.Equals(scopeRequest.ReferenceCategory, StringComparison.OrdinalIgnoreCase);

                // Chỉ lọc Category nếu AI không bị ảo giác
                if (!isAiHallucinating)
                {
                    query = query.Where(i => i.Category == intent.Category);
                }
            }

            if (!string.IsNullOrEmpty(intent.Style) && intent.Style != "General")
            {
                query = query.Where(i => i.Style != null && i.Style.Contains(intent.Style));
            }

            if (!string.IsNullOrEmpty(intent.MainColor))
            {
                query = query.Where(i => i.MainColor == intent.MainColor);
            }

            if (intent.MustExclude != null && intent.MustExclude.Any())
            {
                foreach (var excludeWord in intent.MustExclude)
                {
                    query = query.Where(i =>
                        (i.Material == null || !i.Material.Contains(excludeWord)) &&
                        (i.Style == null || !i.Style.Contains(excludeWord))
                    );
                }
            }

            // --- BƯỚC 3: SẮP XẾP BẰNG VECTOR (SEMANTIC SEARCH) ---
            return await query
                .AsNoTracking()
                .Include(i => i.Images)
                .OrderBy(i => i.ItemEmbedding.CosineDistance(queryVector))
                .Take(scopeRequest.Limit > 0 ? scopeRequest.Limit : 15)
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

