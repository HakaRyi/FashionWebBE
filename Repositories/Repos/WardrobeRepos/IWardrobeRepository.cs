using Repositories.Entities;

namespace Repositories.Repos.WardrobeRepos
{
    public interface IWardrobeRepository
    {
        Task<int> CreateWardrobe(Wardrobe wardrobe);
        Task<List<Wardrobe>> GetAll();
        Task<Wardrobe?> GetByIdAsync(int wardrobeId);
        Task<Wardrobe?> GetByAccountIdAsync(int accountId);
    }
}