using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IItemVariantRepository
    {
        Task<ItemVariant?> GetByIdAsync(int itemVariantId);
        Task<ItemVariant?> GetByIdForUpdateAsync(int itemVariantId);
        Task<List<ItemVariant>> GetByItemIdAsync(int itemId);
        Task<List<ItemVariant>> GetActiveByItemIdAsync(int itemId);
        Task<ItemVariant?> GetActiveVariantAsync(int itemVariantId);
        Task<bool> ExistsSkuAsync(string sku);
        Task<bool> ExistsSkuAsync(int itemId, string sku);
        Task<bool> ExistsOtherSkuAsync(int itemVariantId, int itemId, string sku);
        bool HasEnoughStock(ItemVariant variant, int quantity);
        void ReserveStock(ItemVariant variant, int quantity);
        void ConfirmReservedStock(ItemVariant variant, int quantity);
        void ReleaseReservedStock(ItemVariant variant, int quantity);
        void Restock(ItemVariant variant, int quantity);
        Task AddAsync(ItemVariant variant);
        Task AddRangeAsync(IEnumerable<ItemVariant> variants);
        void Update(ItemVariant variant);
        void Delete(ItemVariant variant);
        Task<int> SaveChangesAsync();
    }
}