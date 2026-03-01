using Repositories.Entities;

namespace Repositories.Repos.UserReportRepos
{
    public interface IUserReportRepository
    {
        Task<UserReport> GetById(int id);
        Task<List<UserReport>> GetUserReports();
    }
}
