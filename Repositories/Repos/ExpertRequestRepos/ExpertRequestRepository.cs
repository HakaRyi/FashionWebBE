using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ExpertRequestRepos
{
    public class ExpertRequestRepository : IExpertRequestRepository
    {
        private readonly FashionDbContext _db;
        public ExpertRequestRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ExpertRequest>> GetAllAsync()
        {
            return await _db.ExpertRequests.ToListAsync();
        }

        public async Task<ExpertRequest?> GetById(int id)
        {
            return await _db.ExpertRequests.FindAsync(id);
        }

        public async Task<ExpertRequest?> GetByProfileIdAsync(int profileId)
        {
            return await _db.ExpertRequests
                .FirstOrDefaultAsync(f => f.ExpertProfileId == profileId);
        }

        public async Task<IEnumerable<ExpertRequest>> GetStatusApplicationsAsync(string status)
        {
            return await _db.ExpertRequests
                .Include(f => f.ExpertProfile)
                .Where(f => f.Status == status)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AnyPendingRequestAsync(int profileId)
        {
            return await _db.ExpertRequests
                .AnyAsync(r => r.ExpertProfileId == profileId && r.Status == "Pending");
        }
        public async Task AddAsync(ExpertRequest file)
        {
            file.CreatedAt = DateTime.UtcNow;
            await _db.ExpertRequests.AddAsync(file);
        }

        public void Update(ExpertRequest file)
        {
            _db.ExpertRequests.Update(file);
        }

        public async Task DeleteAsync(int id)
        {
            var file = await _db.ExpertRequests.FindAsync(id);
            if (file != null)
            {
                _db.ExpertRequests.Remove(file);
            }
        }
    }
}
