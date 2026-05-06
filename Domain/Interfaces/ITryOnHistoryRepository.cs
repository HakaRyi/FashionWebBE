using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ITryOnHistoryRepository
    {
        Task AddAsync(TryOnHistory tryOnHistory);
        Task<List<TryOnHistory>> GetTryOnHistoryByAccountIdAsync(int accountId);
        Task<TryOnHistory?> GetByIdAsync(int tryOnId);
        void Remove(TryOnHistory tryOnHistory);
    }
}