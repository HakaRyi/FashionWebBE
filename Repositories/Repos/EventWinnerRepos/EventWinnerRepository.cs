using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.EventWinnerRepos
{
    public class EventWinnerRepository : IEventWinnerRepository
    {
        private readonly FashionDbContext _context;

        public EventWinnerRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EventWinner winner)
        {
            await _context.EventWinners.AddAsync(winner);
        }

        public async Task AddRangeAsync(IEnumerable<EventWinner> winners)
        {
            await _context.EventWinners.AddRangeAsync(winners);
        }

        public async Task<IEnumerable<EventWinner>> GetByEventIdAsync(int eventId)
        {
            return await _context.EventWinners
                .Include(ew => ew.Account)
                .Include(ew => ew.PrizeEvent)
                .Where(ew => ew.PrizeEvent.EventId == eventId)
                .ToListAsync();
        }

        public async Task<EventWinner?> GetByIdAsync(int eventWinnerId)
        {
            return await _context.EventWinners.FindAsync(eventWinnerId);
        }
    }
}
