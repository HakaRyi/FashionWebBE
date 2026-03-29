namespace Repositories.Repos.OutfitRepos
{
    public interface IOutfitRepository
    {
        Task AddAsync(Entities.Outfit outfit);
    }
}
