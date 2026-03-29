using Repositories.Entities;
using System.Linq.Expressions;

namespace Repositories.Repos.EventExpertRepos
{
    public interface IEventExpertRepository
    {
        Task AddRangeAsync(IEnumerable<EventExpert> experts);
        Task<bool> AnyAsync(Expression<Func<EventExpert, bool>> predicate);
    }
}
