using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.UserReportRepos
{
    public interface IUserReportRepository
    {
        Task<UserReport> GetById(int id);
        Task<List<UserReport>> GetUserReports();
    }
}
