using Application.Request.TryOn;
using Application.Response.TryOn;

namespace Application.Services.TryOn
{
    public interface ITryOnHistoryService
    {
        Task<int> CreateTryOnHistoryAsync(int accountId, CreateHistoryTryOnRequest request);
        Task<List<TryOnHistoryResponse>> GetTryOnHistoryByAccountIdAsync(int accountId);
    }
}
