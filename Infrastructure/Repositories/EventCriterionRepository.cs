using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class EventCriterionRepository : IEventCriterionRepository
    {
        private readonly FashionDbContext _context;

        public EventCriterionRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EventCriterion>> GetCriteriaByEventIdAsync(int eventId)
        {
            return await _context.EventCriterions
                .Where(c => c.EventId == eventId)
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<EventCriterion> criteria)
        {
            await _context.EventCriterions.AddRangeAsync(criteria);
            await _context.SaveChangesAsync();
        }
    }
}
