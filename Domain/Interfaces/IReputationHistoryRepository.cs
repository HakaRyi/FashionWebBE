using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IReputationHistoryRepository
    {
        Task AddAsync(ReputationHistory history);
        Task<IEnumerable<ReputationHistory>> GetByExpertProfileIdAsync(int profileId);
    }
}
