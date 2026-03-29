using Repositories.Entities;

namespace Repositories.Repos.TryOn
{
    public interface ITryOnHistoryRepository
    {
        Task<int> CreateTryOnHistoryAsync(TryOnHistory tryOnHistory);
        Task<List<TryOnHistory>> GetTryOnHistoryByAccountIdAsync(int accountId);
    }
}
