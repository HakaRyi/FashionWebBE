using Repositories.Entities;

namespace Repositories.Repos.WardrobeRepos
{
    public interface IWardrobeRepository
    {
        Task<Wardrobe?> GetById(int accountId);
        Task<int> CreateWardrobe(Wardrobe wardrobe);
        Task<List<Wardrobe>> GetAll();
        Task<Wardrobe?> GetWardrobeByAccount(int accountId);
    }
}
