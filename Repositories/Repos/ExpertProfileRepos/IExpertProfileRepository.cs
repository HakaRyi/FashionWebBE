using Repositories.Entities;

namespace Repositories.Repos.ExpertProfileRepos
{
    public interface IExpertProfileRepository
    {
        Task<IEnumerable<ExpertProfile>> GetAllAsync();
        Task<ExpertProfile?> GetById(int id);
        Task<ExpertProfile?> GetByAccountIdAsync(int accountId);
        Task<IEnumerable<ExpertProfile>> GetAllActiveExpertsAsync();
        Task<ExpertProfile?> GetExpertDetailByIdAsync(int id);
        Task AddAsync(ExpertProfile profile);
        void Update(ExpertProfile profile);
        Task DeleteAsync(int id);
    }
}
