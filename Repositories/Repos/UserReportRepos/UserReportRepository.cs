using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.UserReportRepos
{
    public class UserReportRepository : IUserReportRepository
    {
        private readonly FashionDbContext _db;
        public UserReportRepository(FashionDbContext db)
        {
            _db = db;
        }
        public async Task<UserReport> GetById(int id)
        {
            return await _db.UserReports.FindAsync(id);
        }

        public async Task<List<UserReport>> GetUserReports()
        {
            return await _db.UserReports
                .Include(ur => ur.Account)
                .Include(ur => ur.Post)
                .Include(ur => ur.ReportType)
                .OrderByDescending(ur => ur.CreatedAt)
                .ToListAsync();
        }
    }
}
