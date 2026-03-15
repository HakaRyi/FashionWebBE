using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.EventExpertRepos
{
    public class EventExpertRepository : IEventExpertRepository
    {
        private readonly FashionDbContext _context;
        public EventExpertRepository(FashionDbContext context) => _context = context;

        public async Task AddRangeAsync(IEnumerable<EventExpert> experts) =>
            await _context.EventExperts.AddRangeAsync(experts);

        public async Task<bool> AnyAsync(Expression<Func<EventExpert, bool>> predicate) =>
            await _context.EventExperts.AnyAsync(predicate);
    }
}
