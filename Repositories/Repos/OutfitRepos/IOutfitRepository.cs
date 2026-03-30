using Repositories.Entities;

namespace Repositories.Repos.OutfitRepos

{
    public interface IOutfitRepository
    {
        Task AddAsync(Outfit outfit);
        Task<Outfit> CreateOutfitAsync(Outfit outfit);
        Task<List<Outfit>> GetUserOutfitsAsync(int accountId);
    }
}
