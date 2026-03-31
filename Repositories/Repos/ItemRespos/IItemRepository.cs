using Pgvector;
using Repositories.Dto;
using Repositories.Dto.Wardrobe;
using Repositories.Entities;

namespace Repositories.Repos.ItemRespos
{
    public interface IItemRepository
    {
        Task<Item?> GetByIdAsync(int id);
        Task<List<Item>> GetItemsByIds(List<int> itemIds);
        Task<IEnumerable<Item>> GetAllAsync();
        Task<IEnumerable<Item>> GetByWardrobeIdAsync(int wardrobeId);

        Task<int> CountPublicItemsByAccountIdAsync(int accountId);

        Task<List<PublicWardrobeItemDto>> GetPublicItemsByAccountIdAsync(
            int accountId,
            int page,
            int pageSize);

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