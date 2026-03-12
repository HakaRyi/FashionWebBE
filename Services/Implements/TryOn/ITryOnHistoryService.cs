using Services.Request.TryOn;
using Services.Response.TryOn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.TryOn
{
    public interface ITryOnHistoryService
    {
        Task<int> CreateTryOnHistoryAsync(int accountId, CreateHistoryTryOnRequest request);
        Task<List<TryOnHistoryResponse>> GetTryOnHistoryByAccountIdAsync(int accountId);
    }
}
