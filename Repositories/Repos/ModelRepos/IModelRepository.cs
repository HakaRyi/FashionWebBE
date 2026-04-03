using Repositories.Entities;
namespace Repositories.Repos.ModelRepos
{
    public interface IModelRepository
    {
        Task CreateModelAsync(Model model);
        Task DeleteModelAsync(int modelId);

        Task<IEnumerable<Model>> GetModelsByAccountIdAsync(int accountId);
        Task<Model?> GetModelByIdAsync(int modelId);
        Task UpdateModelAsync(Model model);
    }
}
