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

        public async Task<ExpertProfile> GetById(int id)
        {
            return await _db.ExpertProfiles.FindAsync(id);
        }
    }
}
