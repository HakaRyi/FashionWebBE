using Repositories.Entities;

namespace Repositories.Repos.ReputationHistoryRepos
{
    public interface IReputationHistoryRepository
    {
        Task AddAsync(ReputationHistory history);
        Task<IEnumerable<ReputationHistory>> GetByExpertProfileIdAsync(int profileId);
    }
}
