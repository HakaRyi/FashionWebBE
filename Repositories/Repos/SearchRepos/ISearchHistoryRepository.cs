using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.SearchRepos
{
    public interface ISearchHistoryRepository
    {
        Task<List<SearchHistory>> GetByAccountIdAsync(int accountId);
        Task<SearchHistory?> GetByKeywordAsync(int accountId, string keyword);
        Task AddAsync(SearchHistory searchHistory);
        Task UpdateAsync(SearchHistory searchHistory);
        Task DeleteAllByAccountIdAsync(int accountId);
    }
}
