using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System.Linq.Expressions;

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

        public async Task<IEnumerable<int>> GetEventIdsByExpertIdAsync(int expertId)
        => await _context.EventExperts
            .Where(ee => ee.ExpertId == expertId)
            .Select(ee => ee.EventId)
            .ToListAsync();

        public async Task<IEnumerable<EventExpert>> GetByEventIdAsync(int eventId)
        {
            return await _context.EventExperts
                .Where(ee => ee.EventId == eventId)
                .ToListAsync();
        }

        public async Task<EventExpert?> GetByEventAndExpertAsync(int eventId, int expertId)
        {
            return await _context.EventExperts
                .FirstOrDefaultAsync(ee => ee.EventId == eventId && ee.ExpertId == expertId);
        }

        public async Task<IEnumerable<int>> GetEventIdsByStatusAsync(int expertId, string status)
        {
            return await _context.EventExperts
                .Where(ee => ee.ExpertId == expertId && ee.Status == status)
                .Select(ee => ee.EventId)
                .ToListAsync();
        }

        public async Task<int> CountAcceptedExpertsAsync(int eventId)
        {
            return await _context.EventExperts
                .Where(ee => ee.EventId == eventId && ee.Status == "Accepted")
                .CountAsync();
        }

        public void Update(EventExpert expert) => _context.EventExperts.Update(expert);
    }
}
