using Application.Response.ItemResp;
using Domain.Contracts.Wardrobe;
using Domain.Dto;
using Domain.Dto.Wardrobe;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Infrastructure.Repositories
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
                .AsNoTracking()
                .Include(i => i.Images)
                .Include(i => i.ItemVariants)
                .Include(i => i.Wardrobe)
                    .ThenInclude(w => w.Account)
                .FirstOrDefaultAsync(i =>
                    i.ItemId == id &&
                    i.Status != ItemStatus.Deleted);
        }

        public async Task<Item?> GetByIdForUpdateAsync(int id)
        {
            return await _context.Items
                .Include(i => i.Images)
                .Include(i => i.ItemVariants)
                .Include(i => i.Wardrobe)
                    .ThenInclude(w => w.Account)
                .FirstOrDefaultAsync(i =>
                    i.ItemId == id &&
                    i.Status != ItemStatus.Deleted);
        }

        public async Task<Item?> GetSellableItemByIdAsync(int itemId)
        {
            return await _context.Items
                .AsNoTracking()
                .Include(i => i.Images)
                .Include(i => i.ItemVariants.Where(v =>
                    v.Status == ItemVariantStatus.Active &&
                    v.StockQuantity > v.ReservedQuantity))
                .Include(i => i.Wardrobe)
                    .ThenInclude(w => w.Account)
                .FirstOrDefaultAsync(i =>
                    i.ItemId == itemId &&
                    i.IsPublic == true &&
                    i.IsForSale &&
                    i.Status == ItemStatus.Active &&
                    i.ItemVariants.Any(v =>
                        v.Status == ItemVariantStatus.Active &&
                        v.StockQuantity > v.ReservedQuantity));
        }

        public async Task<List<Item>> GetItemsByIds(List<int> itemIds)
        {
            return await _context.Items
                .AsNoTracking()
                .Where(i =>
                    itemIds.Contains(i.ItemId) &&
                    i.Status != ItemStatus.Deleted)
                .ToListAsync();
        }

        public async Task<List<Item>> GetItemsWithDetailsByIdsAsync(List<int> itemIds)
        {
            return await _context.Items
                .AsNoTracking()
                .Where(i =>
                    itemIds.Contains(i.ItemId) &&
                    i.Status != ItemStatus.Deleted)
                .Include(i => i.Images)
                .Include(i => i.ItemVariants)
                .Include(i => i.Wardrobe)
                    .ThenInclude(w => w.Account)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetAllAsync()
        {
            return await _context.Items
                .AsNoTracking()
                .Where(i => i.Status != ItemStatus.Deleted)
                .Include(i => i.Images)
                .Include(i => i.ItemVariants)
                .Include(i => i.Wardrobe)
                    .ThenInclude(w => w.Account)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetByWardrobeIdAsync(int wardrobeId)
        {
            return await _context.Items
                .AsNoTracking()
                .Where(i =>
                    i.WardrobeId == wardrobeId &&
                    i.Status != ItemStatus.Deleted)
                .Include(i => i.Images)
                .Include(i => i.ItemVariants.Where(v =>
                    v.Status != ItemVariantStatus.Deleted &&
                    v.Status != ItemVariantStatus.Archived))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Item> Items, int TotalCount)> GetByWardrobeIdAsync2(
            int wardrobeId,
            int page,
            int pageSize,
            string? search)
        {
            var query = _context.Items
                .AsNoTracking()
                .Where(i =>
                    i.WardrobeId == wardrobeId &&
                    i.Status != ItemStatus.Deleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();

                query = query.Where(i =>
                    (i.ItemName != null && i.ItemName.Contains(keyword)) ||
                    (i.Style != null && i.Style.Contains(keyword)) ||
                    (i.Category != null && i.Category.Contains(keyword)) ||
                    (i.ItemType != null && i.ItemType.Contains(keyword)) ||
                    (i.Brand != null && i.Brand.Contains(keyword)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(i => i.Images)
                .Include(i => i.ItemVariants.Where(v =>
                    v.Status != ItemVariantStatus.Deleted &&
                    v.Status != ItemVariantStatus.Archived))
                .Include(i => i.Wardrobe)
                    .ThenInclude(w => w.Account)
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Item>> GetSellableItemsByWardrobeIdAsync(int wardrobeId)
        {
            return await _context.Items
                .AsNoTracking()
                .Where(i =>
                    i.WardrobeId == wardrobeId &&
                    i.IsPublic == true &&
                    i.IsForSale &&
                    i.Status == ItemStatus.Active &&
                    i.ItemVariants.Any(v =>
                        v.Status == ItemVariantStatus.Active &&
                        v.StockQuantity > v.ReservedQuantity))
                .Include(i => i.Images)
                .Include(i => i.ItemVariants.Where(v =>
                    v.Status == ItemVariantStatus.Active &&
                    v.StockQuantity > v.ReservedQuantity))
                .OrderByDescending(i => i.PublishedAt ?? i.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CountPublicItemsByAccountIdAsync(int accountId)
        {
            return await _context.Items
                .AsNoTracking()
                .CountAsync(i =>
                    i.Wardrobe.AccountId == accountId &&
                    i.IsPublic == true &&
                    i.Status == ItemStatus.Active);
        }

        public async Task<List<PublicWardrobeItemDto>> GetPublicItemsByAccountIdAsync(
            int accountId,
            int page,
            int pageSize)
        {
            return await _context.Items
                .AsNoTracking()
                .Where(i =>
                    i.Wardrobe.AccountId == accountId &&
                    i.IsPublic == true &&
                    i.Status == ItemStatus.Active)
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new PublicWardrobeItemDto
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Category = i.Category,
                    SubCategory = i.SubCategory,
                    Style = i.Style,
                    Gender = i.Gender,
                    MainColor = i.MainColor,
                    SubColor = i.SubColor,
                    Material = i.Material,
                    Pattern = i.Pattern,
                    Fit = i.Fit,
                    Size = i.Size,
                    Brand = i.Brand,
                    Description = i.Description,
                    CreatedAt = i.CreatedAt,
                    ThumbnailUrl = i.Images
                        .OrderBy(img => img.CreatedAt)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault(),

                    IsForSale = i.IsForSale,
                    ListedPrice = i.ListedPrice,
                    Condition = i.Condition
                })
                .ToListAsync();
        }

        public async Task<PublicWardrobeItemDetailDto?> GetPublicItemDetailAsync(int itemId)
        {
            return await _context.Items
                .AsNoTracking()
                .Where(i =>
                    i.ItemId == itemId &&
                    i.IsPublic == true &&
                    i.Status == ItemStatus.Active)
                .Select(i => new PublicWardrobeItemDetailDto
                {
                    ItemId = i.ItemId,
                    WardrobeId = i.WardrobeId,
                    AccountId = i.Wardrobe.AccountId,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Category = i.Category,
                    SubCategory = i.SubCategory,
                    Style = i.Style,
                    Gender = i.Gender,
                    MainColor = i.MainColor,
                    SubColor = i.SubColor,
                    Material = i.Material,
                    Pattern = i.Pattern,
                    Fit = i.Fit,
                    Neckline = i.Neckline,
                    SleeveLength = i.SleeveLength,
                    Length = i.Length,
                    Size = i.Size,
                    Brand = i.Brand,
                    Description = i.Description,
                    CreatedAt = i.CreatedAt,
                    ImageUrls = i.Images
                        .OrderBy(img => img.CreatedAt)
                        .Select(img => img.ImageUrl)
                        .ToList(),
                    OwnerUserName = i.Wardrobe.Account.UserName,

                    IsForSale = i.IsForSale,
                    ListedPrice = i.ListedPrice,
                    Condition = i.Condition,

                    Variants = i.IsForSale
                        ? i.ItemVariants
                            .Where(v =>
                                v.Status == ItemVariantStatus.Active &&
                                v.StockQuantity > v.ReservedQuantity)
                            .Select(v => new ItemVariantResponseDto
                            {
                                ItemVariantId = v.ItemVariantId,
                                ItemId = v.ItemId,
                                Sku = v.Sku,
                                SizeCode = v.SizeCode,
                                Color = v.Color,
                                Price = v.Price,
                                StockQuantity = v.StockQuantity,
                                ReservedQuantity = v.ReservedQuantity,
                                Status = v.Status,
                                CreatedAt = v.CreatedAt,
                                UpdatedAt = v.UpdatedAt
                            })
                            .ToList()
                        : new List<ItemVariantResponseDto>()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<Item>> GetByVectorSimilarityAsync(Vector embedding, int limit = 20)
        {
            return await _context.Items
                .AsNoTracking()
                .Where(i => i.Status == ItemStatus.Active)
                .Include(i => i.Images)
                .OrderBy(i => i.ItemEmbedding.CosineDistance(embedding))
                .Take(limit)
                .Select(i => new Item
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    MainColor = i.MainColor,
                    Category = i.Category,
                    Size = i.Size,
                    Images = i.Images
                        .OrderBy(img => img.CreatedAt)
                        .Take(1)
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task<List<Item>> GetHybridRecommendationsAsync(
            Vector queryVector,
            SearchIntent intent,
            int currentAccountId,
            SmartRecommendationDto scopeRequest)
        {
            IQueryable<Item> query = _context.Items;

            query = query.Where(item =>
                (scopeRequest.IncludeMyWardrobe && item.Wardrobe.AccountId == currentAccountId) ||

                (scopeRequest.TargetWardrobeIds.Any()
                 && scopeRequest.TargetWardrobeIds.Contains(item.WardrobeId)
                 && item.IsPublic == true) ||

                (scopeRequest.IncludeSavedItems
                 && _context.SavedItems.Any(s =>
                     s.AccountId == currentAccountId &&
                     s.ItemId == item.ItemId))
            );

            query = query.Where(i => i.Status == ItemStatus.Active);

            if (!string.IsNullOrEmpty(scopeRequest.ReferenceCategory))
            {
                query = query.Where(i => i.Category != scopeRequest.ReferenceCategory);

                if (scopeRequest.ReferenceCategory.Equals("full_body", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i =>
                        i.Category != "upper_body" &&
                        i.Category != "lower_body");
                }

                if (scopeRequest.ReferenceCategory.Equals("upper_body", StringComparison.OrdinalIgnoreCase) ||
                    scopeRequest.ReferenceCategory.Equals("lower_body", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.Category != "full_body");
                }
            }

            if (!string.IsNullOrEmpty(intent.Gender) && intent.Gender != "Unknown")
            {
                query = query.Where(i =>
                    i.Gender == intent.Gender ||
                    i.Gender == "Unisex");
            }

            if (!string.IsNullOrEmpty(intent.Category) && intent.Category != "Unknown")
            {
                bool isAiHallucinating =
                    !string.IsNullOrEmpty(scopeRequest.ReferenceCategory) &&
                    intent.Category.Equals(scopeRequest.ReferenceCategory, StringComparison.OrdinalIgnoreCase);

                if (!isAiHallucinating)
                {
                    query = query.Where(i => i.Category == intent.Category);
                }
            }

            if (!string.IsNullOrEmpty(intent.Style) && intent.Style != "General")
            {
                query = query.Where(i =>
                    i.Style != null &&
                    i.Style.Contains(intent.Style));
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
                        (i.Style == null || !i.Style.Contains(excludeWord)));
                }
            }

            return await query
                .AsNoTracking()
                .Include(i => i.Images)
                .Include(i => i.Wardrobe)
                    .ThenInclude(w => w.Account)
                .Include(i => i.ItemVariants.Where(v =>
                    v.Status != ItemVariantStatus.Deleted &&
                    v.Status != ItemVariantStatus.Archived))
                .OrderBy(i => i.ItemEmbedding.CosineDistance(queryVector))
                .Take(scopeRequest.Limit > 0 ? scopeRequest.Limit : 15)
                .ToListAsync();
        }

        public async Task<List<Item>> GetPublicSellableItemsAsync(int page, int pageSize)
        {
            return await _context.Items
                .AsNoTracking()
                .Where(i =>
                    i.IsPublic == true &&
                    i.IsForSale &&
                    i.Status == ItemStatus.Active &&
                    i.ItemVariants.Any(v =>
                        v.Status == ItemVariantStatus.Active &&
                        v.StockQuantity > v.ReservedQuantity))
                .Include(i => i.Images)
                .Include(i => i.ItemVariants.Where(v =>
                    v.Status == ItemVariantStatus.Active &&
                    v.StockQuantity > v.ReservedQuantity))
                .Include(i => i.Wardrobe)
                    .ThenInclude(w => w.Account)
                .OrderByDescending(i => i.PublishedAt ?? i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountPublicSellableItemsAsync()
        {
            return await _context.Items
                .AsNoTracking()
                .CountAsync(i =>
                    i.IsPublic == true &&
                    i.IsForSale &&
                    i.Status == ItemStatus.Active &&
                    i.ItemVariants.Any(v =>
                        v.Status == ItemVariantStatus.Active &&
                        v.StockQuantity > v.ReservedQuantity));
        }

        public async Task AddAsync(Item item)
        {
            item.CreatedAt = DateTime.UtcNow;
            item.UpdateAt = DateTime.UtcNow;

            await _context.Items.AddAsync(item);
        }

        public void Update(Item item)
        {
            item.UpdateAt = DateTime.UtcNow;
            _context.Items.Update(item);
        }

        public void Delete(Item item)
        {
            item.Status = ItemStatus.Deleted;
            item.IsForSale = false;
            item.IsPublic = false;
            item.PublishedAt = null;
            item.UpdateAt = DateTime.UtcNow;

            foreach (var variant in item.ItemVariants)
            {
                if (variant.Status != ItemVariantStatus.Deleted)
                {
                    variant.Status = ItemVariantStatus.Deleted;
                    variant.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.Items.Update(item);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}