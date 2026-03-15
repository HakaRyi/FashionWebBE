using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.PrizeEventRepos
{
    public class PrizeEventRepository : IPrizeEventRepository
    {
        private readonly FashionDbContext _context;
        public PrizeEventRepository(FashionDbContext context) => _context = context;

        public async Task AddRangeAsync(IEnumerable<PrizeEvent> prizes) =>
            await _context.PrizeEvents.AddRangeAsync(prizes);

        public async Task<IEnumerable<PrizeEvent>> GetByEventIdAsync(int eventId) =>
            await _context.PrizeEvents.Where(p => p.EventId == eventId).ToListAsync();

        public async Task<PrizeEvent?> GetByIdAsync(int prizeId) =>
            await _context.PrizeEvents.FindAsync(prizeId);

        public void Update(PrizeEvent prize)
        {
            _context.PrizeEvents.Update(prize);
        }
    }
}
