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
            => await _db.Events.Include(e => e.Posts).FirstOrDefaultAsync(e => e.EventId == id);

        public async Task<IEnumerable<Event>> GetAllByCreatorIdAsync(int creatorId)
            => await _db.Events.Where(e => e.CreatorId == creatorId).ToListAsync();

        public async Task AddAsync(Event @event)
        {
            @event.CreatedAt = DateTime.UtcNow;
            @event.Status = "Active";
            await _db.Events.AddAsync(@event);
        }

        public void Update(Event @event) => _db.Events.Update(@event);

        public void Delete(Event @event) => _db.Events.Remove(@event);

        public async Task<bool> SaveChangesAsync() => (await _db.SaveChangesAsync()) > 0;
    }
}
