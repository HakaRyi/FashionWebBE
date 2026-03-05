using Pgvector;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
