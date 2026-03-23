using Services.Request.TryOn;
using Services.Response.TryOn;

namespace Services.Implements.TryOn
{
    public interface ITryOnHistoryService
    {
        Task<int> CreateTryOnHistoryAsync(int accountId, CreateHistoryTryOnRequest request);
        Task<List<TryOnHistoryResponse>> GetTryOnHistoryByAccountIdAsync(int accountId);
    }
}
