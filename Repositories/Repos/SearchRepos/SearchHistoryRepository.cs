using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.SearchRepos
{
    public class SearchHistoryRepository : ISearchHistoryRepository
    {
        private readonly FashionDbContext _context;

        public SearchHistoryRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<List<SearchHistory>> GetByAccountIdAsync(int accountId)
        {
            return await _context.SearchHistories
                .Where(x => x.AccountId == accountId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<SearchHistory?> GetByKeywordAsync(int accountId, string keyword)
        {
            var lowerKeyword = keyword.ToLower();
            return await _context.SearchHistories
                .FirstOrDefaultAsync(x => x.AccountId == accountId && x.Keyword.ToLower() == lowerKeyword);
        }

        public async Task AddAsync(SearchHistory searchHistory)
        {
            await _context.SearchHistories.AddAsync(searchHistory);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SearchHistory searchHistory)
        {
            _context.SearchHistories.Update(searchHistory);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllByAccountIdAsync(int accountId)
        {
            var histories = await _context.SearchHistories.Where(x => x.AccountId == accountId).ToListAsync();
            if (histories.Any())
            {
                _context.SearchHistories.RemoveRange(histories);
                await _context.SaveChangesAsync();
            }
        }
    }
}
