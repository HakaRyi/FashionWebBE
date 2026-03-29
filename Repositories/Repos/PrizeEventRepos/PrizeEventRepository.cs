using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.PrizeEventRepos
{
    public class PrizeEventRepository : IPrizeEventRepository
    {
        private readonly FashionDbContext _context;
        public PrizeEventRepository(FashionDbContext context) => _context = context;

        public async Task AddRangeAsync(IEnumerable<PrizeEvent> prizes) =>
            await _context.PrizeEvents.AddRangeAsync(prizes);

        public async Task<IEnumerable<PrizeEvent>> GetByEventIdAsync(int eventId) =>
            await _context.PrizeEvents
                .Where(p => p.EventId == eventId && p.Status == "Active")
                .ToListAsync();

        public async Task<PrizeEvent?> GetByIdAsync(int prizeId) =>
            await _context.PrizeEvents.FindAsync(prizeId);

        public void Update(PrizeEvent prize) => _context.PrizeEvents.Update(prize);
    }
}
