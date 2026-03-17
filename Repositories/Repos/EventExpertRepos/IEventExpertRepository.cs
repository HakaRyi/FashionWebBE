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
    }
}
