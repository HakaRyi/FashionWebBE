using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.TryOn
{
    public interface ITryOnHistoryRepository
    {
        Task<int> CreateTryOnHistoryAsync(TryOnHistory tryOnHistory);
        Task<List<TryOnHistory>> GetTryOnHistoryByAccountIdAsync(int accountId);
    }
}
