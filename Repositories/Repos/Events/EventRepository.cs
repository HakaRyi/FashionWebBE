using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.Events
{
    public class EventRepository : IEventRepository
    {
        private readonly FashionDbContext _db;
        public EventRepository(FashionDbContext db) => _db = db;

        public async Task<Event?> GetByIdAsync(int id)
            => await _db.Events
                .Include(e => e.Creator)
                .Include(e => e.PrizeEvents)
                .Include(e => e.EventExperts)
                    .ThenInclude(ee => ee.Expert)
                .FirstOrDefaultAsync(e => e.EventId == id);

        public async Task<IEnumerable<Event>> GetAllByCreatorIdAsync(int creatorId)
            => await _db.Events
                .Include(e => e.Creator)
                .Include(e => e.Posts)
                .Where(e => e.CreatorId == creatorId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Event>> GetAnalyticsDataAsync(int creatorId, DateTime startDate)
        {
            return await _db.Events
                .Where(e => e.CreatorId == creatorId && e.CreatedAt >= startDate)
                .Include(e => e.Posts)
                    .ThenInclude(p => p.Scoreboard)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }


        public async Task AddAsync(Event @event)
        {
            await _db.Events.AddAsync(@event);
        }

        public void Update(Event @event) => _db.Events.Update(@event);

        public void Delete(Event @event) => _db.Events.Remove(@event);

        public async Task<IEnumerable<Event>> GetAllAsync()
            => await _db.Events
                .Include(e => e.Creator) // Load để có CreatorName ngoài danh sách
                .Include(e => e.Images)
                .Include(e => e.Posts)   // Để tính ParticipantCount
                .OrderByDescending(e => e.StartTime)
                .ToListAsync();
    }
}
