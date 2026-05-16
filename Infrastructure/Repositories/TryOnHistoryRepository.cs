using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class TryOnHistoryRepository : ITryOnHistoryRepository
    {
        private readonly FashionDbContext _context;

        public TryOnHistoryRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TryOnHistory tryOnHistory)
        {
            await _context.TryOnHistories.AddAsync(tryOnHistory);
        }

        public async Task<List<TryOnHistory>> GetTryOnHistoryByAccountIdAsync(int accountId)
        {
            return await _context.TryOnHistories
                .AsNoTracking()
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<TryOnHistory?> GetByIdAsync(int tryOnId)
        {
            return await _context.TryOnHistories
                .FirstOrDefaultAsync(t => t.TryOnId == tryOnId);
        }

        public void Remove(TryOnHistory tryOnHistory)
        {
            _context.TryOnHistories.Remove(tryOnHistory);
        }
    }
}