using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class EscrowSessionRepository : IEscrowSessionRepository
    {
        private readonly FashionDbContext _context;

        public EscrowSessionRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<EscrowSession> AddAsync(EscrowSession session)
        {
            await _context.EscrowSessions.AddAsync(session);
            return session;
        }

        public async Task<EscrowSession?> GetByIdAsync(int sessionId)
        {
            return await _context.EscrowSessions
                .FirstOrDefaultAsync(x => x.EscrowSessionId == sessionId);
        }


        public async Task<EscrowSession?> GetActiveEscrowByEventIdAsync(int eventId)
        {
            return await _context.EscrowSessions
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.Status == "Held");
        }


        public async Task<EscrowSession?> GetByOrderIdAsync(int orderId)
        {
            return await _context.EscrowSessions
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
        }

        public async Task<EscrowSession?> GetByEventIdAsync(int eventId)
        {
            return await _context.EscrowSessions
                .FirstOrDefaultAsync(x => x.EventId == eventId);
        }

        public void Update(EscrowSession session)
        {
            _context.EscrowSessions.Update(session);
        }

        public IQueryable<EscrowSession> Query()
        {
            return _context.EscrowSessions.AsQueryable();
        }

    }
}