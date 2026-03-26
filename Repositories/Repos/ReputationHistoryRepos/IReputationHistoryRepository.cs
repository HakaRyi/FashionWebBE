using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.ReputationHistoryRepos
{
    public interface IReputationHistoryRepository
    {
        Task AddAsync(ReputationHistory history);
        Task<IEnumerable<ReputationHistory>> GetByExpertProfileIdAsync(int profileId);
    }
}
