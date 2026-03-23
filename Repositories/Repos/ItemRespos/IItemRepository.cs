using Pgvector;
using Repositories.Entities;

namespace Repositories.Repos.ItemRespos
{
    public interface IItemRepository
    {
        Task<Item?> GetByIdAsync(int id);
        Task<IEnumerable<Item>> GetAllAsync();
        Task<IEnumerable<Item>> GetByWardrobeIdAsync(int wardrobeId);

        Task<List<Item>> GetByVectorSimilarityAsync(Vector embedding, int limit = 20);

        Task AddAsync(Item item);
        void Update(Item item);
        void Delete(Item item);

        Task<int> SaveChangesAsync();
    }
}
