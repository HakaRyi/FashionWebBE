using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ItemVariantRepository : IItemVariantRepository
    {
        private readonly FashionDbContext _context;

        public ItemVariantRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<ItemVariant?> GetByIdAsync(int itemVariantId)
        {
            return await _context.ItemVariants
                .AsNoTracking()
                .Include(v => v.Item)
                    .ThenInclude(i => i.Wardrobe)
                .FirstOrDefaultAsync(v => v.ItemVariantId == itemVariantId);
        }

        public async Task<ItemVariant?> GetByIdForUpdateAsync(int itemVariantId)
        {
            return await _context.ItemVariants
                .Include(v => v.Item)
                    .ThenInclude(i => i.Wardrobe)
                .FirstOrDefaultAsync(v => v.ItemVariantId == itemVariantId);
        }

        public async Task<List<ItemVariant>> GetByItemIdAsync(int itemId)
        {
            return await _context.ItemVariants
                .AsNoTracking()
                .Where(v =>
                    v.ItemId == itemId &&
                    v.Status != ItemVariantStatus.Deleted &&
                    v.Status != ItemVariantStatus.Archived)
                .OrderBy(v => v.SizeCode)
                .ThenBy(v => v.Color)
                .ToListAsync();
        }

        public async Task<List<ItemVariant>> GetActiveByItemIdAsync(int itemId)
        {
            return await _context.ItemVariants
                .AsNoTracking()
                .Where(v =>
                    v.ItemId == itemId &&
                    v.Status == ItemVariantStatus.Active &&
                    v.StockQuantity > v.ReservedQuantity)
                .OrderBy(v => v.SizeCode)
                .ThenBy(v => v.Color)
                .ToListAsync();
        }

        public async Task<ItemVariant?> GetActiveVariantAsync(int itemVariantId)
        {
            return await _context.ItemVariants
                .AsNoTracking()
                .Include(v => v.Item)
                    .ThenInclude(i => i.Wardrobe)
                .FirstOrDefaultAsync(v =>
                    v.ItemVariantId == itemVariantId &&
                    v.Status == ItemVariantStatus.Active &&
                    v.StockQuantity > v.ReservedQuantity &&
                    v.Item.IsPublic == true &&
                    v.Item.IsForSale &&
                    v.Item.Status == ItemStatus.Active);
        }

        public async Task<bool> ExistsSkuAsync(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;

            string normalizedSku = sku.Trim().ToLower();

            return await _context.ItemVariants
                .AsNoTracking()
                .AnyAsync(v =>
                    v.Sku.ToLower() == normalizedSku &&
                    v.Status != ItemVariantStatus.Deleted &&
                    v.Status != ItemVariantStatus.Archived);
        }

        public async Task<bool> ExistsSkuAsync(int itemId, string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;

            string normalizedSku = sku.Trim().ToLower();

            return await _context.ItemVariants
                .AsNoTracking()
                .AnyAsync(v =>
                    v.ItemId == itemId &&
                    v.Sku.ToLower() == normalizedSku &&
                    v.Status != ItemVariantStatus.Deleted &&
                    v.Status != ItemVariantStatus.Archived);
        }

        public async Task<bool> ExistsOtherSkuAsync(int itemVariantId, int itemId, string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;

            string normalizedSku = sku.Trim().ToLower();

            return await _context.ItemVariants
                .AsNoTracking()
                .AnyAsync(v =>
                    v.ItemVariantId != itemVariantId &&
                    v.ItemId == itemId &&
                    v.Sku.ToLower() == normalizedSku &&
                    v.Status != ItemVariantStatus.Deleted &&
                    v.Status != ItemVariantStatus.Archived);
        }

        public bool HasEnoughStock(ItemVariant variant, int quantity)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            if (quantity <= 0)
                return false;

            if (variant.Status != ItemVariantStatus.Active)
                return false;

            return variant.StockQuantity - variant.ReservedQuantity >= quantity;
        }

        public void ReserveStock(ItemVariant variant, int quantity)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            if (variant.Status != ItemVariantStatus.Active)
                throw new InvalidOperationException("Only active variant can be reserved.");

            if (!HasEnoughStock(variant, quantity))
                throw new InvalidOperationException("Not enough stock to reserve.");

            variant.ReservedQuantity += quantity;

            if (variant.StockQuantity <= variant.ReservedQuantity)
            {
                variant.Status = ItemVariantStatus.OutOfStock;
            }

            variant.UpdatedAt = DateTime.UtcNow;
            _context.ItemVariants.Update(variant);
        }

        public void ConfirmReservedStock(ItemVariant variant, int quantity)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            if (variant.ReservedQuantity < quantity)
                throw new InvalidOperationException("Reserved quantity is not enough.");

            if (variant.StockQuantity < quantity)
                throw new InvalidOperationException("Stock quantity is not enough.");

            variant.StockQuantity -= quantity;
            variant.ReservedQuantity -= quantity;

            if (variant.StockQuantity < 0)
            {
                variant.StockQuantity = 0;
            }

            if (variant.StockQuantity <= variant.ReservedQuantity)
            {
                variant.Status = ItemVariantStatus.OutOfStock;
            }

            variant.UpdatedAt = DateTime.UtcNow;
            _context.ItemVariants.Update(variant);
        }

        public void ReleaseReservedStock(ItemVariant variant, int quantity)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            if (variant.ReservedQuantity < quantity)
                throw new InvalidOperationException("Reserved quantity is not enough.");

            variant.ReservedQuantity -= quantity;

            if (variant.Status == ItemVariantStatus.OutOfStock &&
                variant.StockQuantity > variant.ReservedQuantity)
            {
                variant.Status = ItemVariantStatus.Active;
            }

            variant.UpdatedAt = DateTime.UtcNow;
            _context.ItemVariants.Update(variant);
        }

        public void Restock(ItemVariant variant, int quantity)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            if (variant.Status == ItemVariantStatus.Deleted ||
                variant.Status == ItemVariantStatus.Archived)
            {
                throw new InvalidOperationException("Cannot restock deleted or archived variant.");
            }

            variant.StockQuantity += quantity;

            if (variant.Status == ItemVariantStatus.OutOfStock &&
                variant.StockQuantity > variant.ReservedQuantity)
            {
                variant.Status = ItemVariantStatus.Active;
            }

            variant.UpdatedAt = DateTime.UtcNow;
            _context.ItemVariants.Update(variant);
        }

        public async Task AddAsync(ItemVariant variant)
        {
            variant.CreatedAt = DateTime.UtcNow;
            variant.UpdatedAt = null;

            if (variant.StockQuantity <= variant.ReservedQuantity)
            {
                variant.Status = ItemVariantStatus.OutOfStock;
            }

            await _context.ItemVariants.AddAsync(variant);
        }

        public async Task AddRangeAsync(IEnumerable<ItemVariant> variants)
        {
            var variantList = variants.ToList();

            foreach (var variant in variantList)
            {
                variant.CreatedAt = DateTime.UtcNow;
                variant.UpdatedAt = null;

                if (variant.StockQuantity <= variant.ReservedQuantity)
                {
                    variant.Status = ItemVariantStatus.OutOfStock;
                }
            }

            await _context.ItemVariants.AddRangeAsync(variantList);
        }

        public void Update(ItemVariant variant)
        {
            variant.UpdatedAt = DateTime.UtcNow;
            _context.ItemVariants.Update(variant);
        }

        public void Delete(ItemVariant variant)
        {
            variant.Status = ItemVariantStatus.Deleted;
            variant.UpdatedAt = DateTime.UtcNow;

            _context.ItemVariants.Update(variant);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}