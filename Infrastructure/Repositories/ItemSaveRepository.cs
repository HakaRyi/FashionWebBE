using Domain.Contracts.Wardrobe;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ItemSaveRepository : IItemSaveRepository
    {
        private readonly FashionDbContext _db;

        public ItemSaveRepository(FashionDbContext db)
        {
            _db = db;
        }

        public Task DeleteSaveItem(SavedItem savedItem)
        {
            _db.SavedItems.Remove(savedItem);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<SavedItem>> GetMySaveItems(int accId)
        {
            return await _db.SavedItems
                .Include(s => s.Item)
                    .ThenInclude(i => i.Images)
                .Include(s => s.Item)
                    .ThenInclude(i=>i.Wardrobe)
                        .ThenInclude(w => w.Account)
                .Where(s => s.AccountId == accId)
                .ToListAsync();
        }

        public async Task<List<int>> GetSavedItemIdsByAccountIdAsync(int accId)
        {
            return await _db.SavedItems
                .Where(s => s.AccountId == accId)
                .Select(s => s.ItemId)
                .ToListAsync();
        }

        public async Task<SavedItem?> GetSaveItem(int itemId, int accId)
        {
            return await _db.SavedItems
                .Include(s => s.Item)
                    .ThenInclude(i => i.Images)
                .Include(s => s.Account)
                .FirstOrDefaultAsync(s => s.ItemId == itemId && s.AccountId == accId);
        }

        public async Task SaveItem(SavedItem savedItem)
        {
            savedItem.SavedAt = DateTime.UtcNow;
            await _db.SavedItems.AddAsync(savedItem);
        }

        public async Task<List<PublicWardrobeItemDto>> GetMySavedItemDtosAsync(int accId)
        {
            return await _db.SavedItems
                .AsNoTracking()
                .Where(s =>
                    s.AccountId == accId &&
                    s.Item.Status == ItemStatus.Active)
                .OrderByDescending(s => s.SavedAt)
                .Select(s => new PublicWardrobeItemDto
                {
                    ItemId = s.Item.ItemId,
                    ItemName = s.Item.ItemName,
                    ItemType = s.Item.ItemType,
                    Category = s.Item.Category,
                    SubCategory = s.Item.SubCategory,
                    Style = s.Item.Style,
                    Gender = s.Item.Gender,
                    MainColor = s.Item.MainColor,
                    SubColor = s.Item.SubColor,
                    Material = s.Item.Material,
                    Pattern = s.Item.Pattern,
                    Fit = s.Item.Fit,
                    Size = s.Item.Size,
                    Brand = s.Item.Brand,
                    Description = s.Item.Description,
                    CreatedAt = s.Item.CreatedAt,
                    ThumbnailUrl = s.Item.Images
                        .OrderBy(img => img.CreatedAt)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault(),

                    IsForSale = s.Item.IsForSale,
                    ListedPrice = s.Item.ListedPrice,
                    Condition = s.Item.Condition,

                    IsSaved = true,
                    IsOwner = s.Item.Wardrobe.AccountId == accId
                })
                .ToListAsync();
        }
    }
}