using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
