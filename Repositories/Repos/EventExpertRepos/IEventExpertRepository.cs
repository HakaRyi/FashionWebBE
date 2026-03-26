using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.EventExpertRepos
{
    public interface IEventExpertRepository
    {
        Task AddRangeAsync(IEnumerable<EventExpert> experts);
        Task<bool> AnyAsync(Expression<Func<EventExpert, bool>> predicate);
        Task<IEnumerable<int>> GetEventIdsByExpertIdAsync(int expertId);
        Task<IEnumerable<int>> GetEventIdsByStatusAsync(int expertId, string status);
        Task<IEnumerable<EventExpert>> GetByEventIdAsync(int eventId);
        Task<EventExpert?> GetByEventAndExpertAsync(int eventId, int expertId);
        Task<int> CountAcceptedExpertsAsync(int eventId);
        void Update(EventExpert expert);
    }
}
