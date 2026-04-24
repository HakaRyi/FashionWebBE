using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

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
                .Where(v => v.ItemId == itemId)
                .OrderBy(v => v.SizeCode)
                .ThenBy(v => v.Color)
                .ToListAsync();
        }

        public async Task<List<ItemVariant>> GetActiveByItemIdAsync(int itemId)
        {
            return await _context.ItemVariants
                .AsNoTracking()
                .Where(v => v.ItemId == itemId && v.Status == ItemVariantStatus.Active)
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
                    v.Status == ItemVariantStatus.Active);
        }

        public async Task<bool> ExistsSkuAsync(string sku)
        {
            return await _context.ItemVariants
                .AsNoTracking()
                .AnyAsync(v => v.Sku == sku);
        }

        public async Task<bool> ExistsSkuAsync(int itemId, string sku)
        {
            return await _context.ItemVariants
                .AsNoTracking()
                .AnyAsync(v => v.ItemId == itemId && v.Sku == sku);
        }

        public bool HasEnoughStock(ItemVariant variant, int quantity)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            if (quantity <= 0)
                return false;

            return variant.StockQuantity - variant.ReservedQuantity >= quantity;
        }

        public void ReserveStock(ItemVariant variant, int quantity)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            if (!HasEnoughStock(variant, quantity))
                throw new InvalidOperationException("Not enough stock to reserve.");

            variant.ReservedQuantity += quantity;
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

            if (variant.StockQuantity <= 0)
            {
                variant.StockQuantity = 0;
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

            if (variant.Status == ItemVariantStatus.OutOfStock && variant.StockQuantity > 0)
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

            variant.StockQuantity += quantity;

            if (variant.Status == ItemVariantStatus.OutOfStock && variant.StockQuantity > 0)
            {
                variant.Status = ItemVariantStatus.Active;
            }

            variant.UpdatedAt = DateTime.UtcNow;
            _context.ItemVariants.Update(variant);
        }

        public async Task AddAsync(ItemVariant variant)
        {
            variant.CreatedAt = DateTime.UtcNow;
            await _context.ItemVariants.AddAsync(variant);
        }

        public async Task AddRangeAsync(IEnumerable<ItemVariant> variants)
        {
            var variantList = variants.ToList();

            foreach (var variant in variantList)
            {
                variant.CreatedAt = DateTime.UtcNow;
            }

            await _context.ItemVariants.AddRangeAsync(variantList);
        }

        public async Task<bool> ExistsOtherSkuAsync(int itemVariantId, int itemId, string sku)
        {
            return await _context.ItemVariants
                .AsNoTracking()
                .AnyAsync(v =>
                    v.ItemVariantId != itemVariantId &&
                    v.ItemId == itemId &&
                    v.Sku == sku);
        }

        public void Update(ItemVariant variant)
        {
            variant.UpdatedAt = DateTime.UtcNow;
            _context.ItemVariants.Update(variant);
        }

        public void Delete(ItemVariant variant)
        {
            _context.ItemVariants.Remove(variant);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}