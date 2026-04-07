using Pgvector;
using Domain.Dto;
using Domain.Dto.Wardrobe;
using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IItemRepository
    {
        Task<Item?> GetByIdAsync(int id);
        Task<Item?> GetByIdForUpdateAsync(int id);
        Task<List<Item>> GetItemsByIds(List<int> itemIds);
        Task<List<Item>> GetItemsWithDetailsByIdsAsync(List<int> itemIds);
        Task<IEnumerable<Item>> GetAllAsync();
        Task<IEnumerable<Item>> GetByWardrobeIdAsync(int wardrobeId);
        Task<int> CountPublicItemsByAccountIdAsync(int accountId);
        Task<List<PublicWardrobeItemDto>> GetPublicItemsByAccountIdAsync(int accountId, int page, int pageSize);
        Task<PublicWardrobeItemDetailDto?> GetPublicItemDetailAsync(int itemId);
        Task<List<Item>> GetByVectorSimilarityAsync(Vector embedding, int limit = 20);
        Task<List<Item>> GetHybridRecommendationsAsync(
            Vector queryVector,
            SearchIntent intent,
            int currentAccountId,
            SmartRecommendationDto scopeRequest);
        Task AddAsync(Item item);
        void Update(Item item);
        void Delete(Item item);
        Task<int> SaveChangesAsync();
    }
}