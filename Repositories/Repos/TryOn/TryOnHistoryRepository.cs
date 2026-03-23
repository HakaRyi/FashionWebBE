using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.TryOn
{
    public class TryOnHistoryRepository : ITryOnHistoryRepository
    {
        private readonly FashionDbContext _context;
        public TryOnHistoryRepository(FashionDbContext context)
        {
            _context = context;
        }
        public async Task<int> CreateTryOnHistoryAsync(TryOnHistory tryOnHistory)
        {
            var result = await _context.TryOnHistories.AddAsync(tryOnHistory);
            return await _context.SaveChangesAsync();
        }

        public Task<List<TryOnHistory>> GetTryOnHistoryByAccountIdAsync(int accountId)
        {
            return Task.FromResult(_context.TryOnHistories.Where(t => t.AccountId == accountId).ToList());
        }
    }
}
