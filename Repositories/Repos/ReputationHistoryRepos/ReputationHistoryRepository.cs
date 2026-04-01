using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ReputationHistoryRepos
{
    public class ReputationHistoryRepository : IReputationHistoryRepository
    {
        private readonly FashionDbContext _context;

        public ReputationHistoryRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ReputationHistory history)
        {
            await _context.Set<ReputationHistory>().AddAsync(history);
        }

        public async Task<IEnumerable<ReputationHistory>> GetByExpertProfileIdAsync(int profileId)
        {
            return await _context.Set<ReputationHistory>()
                .Where(h => h.ExpertProfileId == profileId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }
    }
}
