using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ExpertFileRepos
{
    public class ExpertFileRepository : IExpertFileRepository
    {
        private readonly FashionDbContext _db;
        public ExpertFileRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ExpertFile>> GetAllAsync()
        {
            return await _db.ExpertFiles.ToListAsync();
        }

        public async Task<ExpertFile?> GetById(int id)
        {
            return await _db.ExpertFiles.FindAsync(id);
        }

        public async Task<ExpertFile?> GetByProfileIdAsync(int profileId)
        {
            return await _db.ExpertFiles
                .FirstOrDefaultAsync(f => f.ExpertProfileId == profileId);
        }

        public async Task<IEnumerable<ExpertFile>> GetStatusApplicationsAsync(string status)
        {
            return await _db.ExpertFiles
                .Include(f => f.ExpertProfile)
                .Where(f => f.Status == status)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(ExpertFile file)
        {
            file.CreatedAt = DateTime.UtcNow;
            await _db.ExpertFiles.AddAsync(file);
        }

        public void Update(ExpertFile file)
        {
            _db.ExpertFiles.Update(file);
        }

        public async Task DeleteAsync(int id)
        {
            var file = await _db.ExpertFiles.FindAsync(id);
            if (file != null)
            {
                _db.ExpertFiles.Remove(file);
            }
        }
    }
}
