using Domain.Entities;

namespace Domain.Interfaces

{
    public interface ITryOnHistoryRepository
    {
        Task<int> CreateTryOnHistoryAsync(TryOnHistory tryOnHistory);
        Task<List<TryOnHistory>> GetTryOnHistoryByAccountIdAsync(int accountId);
    }
}
