using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RecommendationHistoryRepository : IRecommendationHistoryRepository
    {
        private readonly FashionDbContext _db;
        public RecommendationHistoryRepository(FashionDbContext db) => _db = db;

        public async Task AddAsync(RecommendationHistory history)
        {
            await _db.RecommendationHistories.AddAsync(history);
        }

        public async Task<List<RecommendationHistory>> GetMyRecommendationHistories(int accountId)
        {
            return await _db.RecommendationHistories
                .Include(h => h.RecommendedItems)
                .Include(h => h.ReferenceItem)
                    .ThenInclude(i => i.Images)
                .Include(h => h.Account)
                    .ThenInclude(a => a.Avatars)
                .Where(h => h.AccountId == accountId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task<RecommendationHistory> GetRecommendationHistoryByIdAsync(int id)
        {
            return await _db.RecommendationHistories
                .Include(h => h.RecommendedItems)
                    .ThenInclude(d => d.Item)
                        .ThenInclude(i => i.Images)
                .Include(h => h.RecommendedItems)
                    .ThenInclude(d => d.Item)
                        .ThenInclude(i => i.Wardrobe)
                            .ThenInclude(w => w.Account)
                .Include(h => h.ReferenceItem)
                    .ThenInclude(i => i.Images)
                .Include(h => h.Account)
                    .ThenInclude(a => a.Avatars)
                .FirstOrDefaultAsync(h => h.Id == id);
        }
    }
}
