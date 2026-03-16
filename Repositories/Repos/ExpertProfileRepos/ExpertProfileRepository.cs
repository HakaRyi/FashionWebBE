using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ExpertProfileRepos
{
    public class ExpertProfileRepository : IExpertProfileRepository
    {
        private readonly FashionDbContext _db;
        public ExpertProfileRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ExpertProfile>> GetAllAsync()
        {
            return await _db.ExpertProfiles
                .Include(p => p.ExpertRequests)
                .ToListAsync();
        }


        public async Task<ExpertProfile?> GetById(int id)
        {
            return await _db.ExpertProfiles
                .Include(p => p.Account)
                .Include(p => p.ExpertRequests)
                .FirstOrDefaultAsync(p => p.ExpertProfileId == id);
        }

        public async Task<ExpertProfile?> GetByAccountIdAsync(int accountId)
        {
            return await _db.ExpertProfiles
                .FirstOrDefaultAsync(p => p.AccountId == accountId);
        }

        public async Task AddAsync(ExpertProfile profile)
        {
            profile.CreatedAt = DateTime.UtcNow;
            await _db.ExpertProfiles.AddAsync(profile);
        }

        public void Update(ExpertProfile profile)
        {
            profile.UpdatedAt = DateTime.UtcNow;
            _db.ExpertProfiles.Update(profile);
        }

        public async Task DeleteAsync(int id)
        {
            var profile = await _db.ExpertProfiles.FindAsync(id);
            if (profile != null)
            {
                _db.ExpertProfiles.Remove(profile);
            }
        }
    }
}
